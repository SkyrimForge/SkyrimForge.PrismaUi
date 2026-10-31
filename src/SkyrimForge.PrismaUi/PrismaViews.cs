using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.PrismaUi;

internal static unsafe class PrismaViews
{
    private static readonly Lock gate = new();
    private static readonly Dictionary<ulong, PrismaView> views = [];
    private static readonly ConcurrentDictionary<uint, Callback> scriptResults = new();
    private static readonly ConcurrentDictionary<uint, Callback> listeners = new();
    private static readonly ConcurrentStack<uint> freeScriptResultSlots = new();
    private static readonly ConcurrentStack<uint> freeListenerSlots = new();

    private static bool handlersInstalled;

    internal static PrismaUiAvailability Acquire()
    {
        lock (gate)
        {
            PrismaUiAvailability availability = NativeBindings.SFP_Acquire() switch
            {
                0 => PrismaUiAvailability.Available,
                1 => PrismaUiAvailability.AvailableWithoutConsoleMessages,
                2 => PrismaUiAvailability.PluginNotLoaded,
                3 => PrismaUiAvailability.PluginHasNoEntryPoint,
                _ => PrismaUiAvailability.PluginReturnedNoInterface
            };

            if (availability is PrismaUiAvailability.Available or PrismaUiAvailability.AvailableWithoutConsoleMessages && !handlersInstalled)
            {
                if (!NativeBindings.SFP_SetHandlers(&OnDomReady, &OnScriptResult, &OnListener, &OnConsoleMessage))
                {
                    return PrismaUiAvailability.LoadedMoreThanOnce;
                }

                uint[] slots = [.. Enumerable.Range(0, (int)NativeBindings.SFP_SlotCount()).Select(i => (uint)i)];
                freeScriptResultSlots.PushRange(slots);
                freeListenerSlots.PushRange(slots);
                handlersInstalled = true;
            }

            return availability;
        }
    }

    // Locked so DOM ready cannot arrive before the view is registered.
    internal static PrismaView? CreateView(string htmlPath, ILog log)
    {
        lock (gate)
        {
            ulong handle = NativeBindings.SFP_CreateView(htmlPath, true);
            if (handle == 0)
            {
                return null;
            }

            var view = new PrismaView(handle, log);
            views[handle] = view;
            NativeBindings.SFP_RegisterConsoleCallback(handle, true);

            view.ConsoleMessage.Subscribe(args =>
            {
                if (args.Level is ConsoleMessageLevel.Warning or ConsoleMessageLevel.Error)
                {
                    log.Write($"The page at {htmlPath} logged {args.Level}: {args.Text}");
                }
            });

            return view;
        }
    }

    internal static void Remove(PrismaView view)
    {
        List<Callback> dropped = [];
        lock (gate)
        {
            views.Remove(view.Handle);
            foreach (uint slot in view.ListenerSlots.Values)
            {
                listeners.TryRemove(slot, out _);
                freeListenerSlots.Push(slot);
            }
            view.ListenerSlots.Clear();

            foreach (KeyValuePair<uint, Callback> pending in scriptResults)
            {
                if (pending.Value.Handle == view.Handle && scriptResults.TryRemove(pending))
                {
                    freeScriptResultSlots.Push(pending.Key);
                    dropped.Add(pending.Value);
                }
            }
        }

        // PrismaUI drops a destroyed view's results, so complete them here.
        foreach (Callback callback in dropped)
        {
            Run(callback.Log, () => callback.Handler(string.Empty));
        }
    }

    internal static ISubscription? RegisterListener(PrismaView view, string functionName, Action<string> handler)
    {
        lock (gate)
        {
            if (!view.ListenerSlots.TryGetValue(functionName, out uint slot))
            {
                if (!freeListenerSlots.TryPop(out slot))
                {
                    return null;
                }
                view.ListenerSlots[functionName] = slot;
            }

            var callback = new Callback(view.Handle, handler, view.Log);
            listeners[slot] = callback;
            NativeBindings.SFP_RegisterJSListener(view.Handle, functionName, slot);
            return new ListenerSubscription(slot, callback);
        }
    }

    private sealed class ListenerSubscription(uint slot, Callback callback) : ISubscription
    {
        public void Unsubscribe()
        {
            // A later registration can reuse this slot, so match the callback too.
            listeners.TryRemove(new KeyValuePair<uint, Callback>(slot, callback));
        }
    }

    // Locked so Remove cannot recycle the slot before the script is submitted.
    internal static Task<string> Invoke(ulong handle, string script, ILog log)
    {
        lock (gate)
        {
            if (!freeScriptResultSlots.TryPop(out uint slot))
            {
                string message = $"No free callback slot, so the script was not evaluated. {NativeBindings.SFP_SlotCount()} results are already outstanding.";
                log.Write(message);
                return Task.FromException<string>(new InvalidOperationException(message));
            }

            var taskCompletionSource = new TaskCompletionSource<string>();
            scriptResults[slot] = new Callback(handle, text => taskCompletionSource.TrySetResult(text), log);
            NativeBindings.SFP_Invoke(handle, script, slot);
            return taskCompletionSource.Task;
        }
    }

    [UnmanagedCallersOnly]
    private static void OnDomReady(ulong handle)
    {
        PrismaView? view;
        lock (gate)
        {
            views.TryGetValue(handle, out view);
        }

        view?.DomReady.Raise(new DomReadyEventArgs(view));
    }

    [UnmanagedCallersOnly]
    private static void OnScriptResult(uint slot, byte* result)
    {
        if (scriptResults.TryRemove(slot, out Callback? callback))
        {
            freeScriptResultSlots.Push(slot);
            string text = Text(result);
            Run(callback.Log, () => callback.Handler(text));
        }
    }

    [UnmanagedCallersOnly]
    private static void OnListener(uint slot, byte* argument)
    {
        if (listeners.TryGetValue(slot, out Callback? callback))
        {
            string text = Text(argument);
            Run(callback.Log, () => callback.Handler(text));
        }
    }

    [UnmanagedCallersOnly]
    private static void OnConsoleMessage(ulong handle, byte level, byte* message)
    {
        PrismaView? view;
        lock (gate)
        {
            views.TryGetValue(handle, out view);
        }

        view?.ConsoleMessage.Raise(new ConsoleMessageEventArgs((ConsoleMessageLevel)level, Text(message)));
    }

    // An exception that reaches native code ends the process.
    private static void Run(ILog log, Action work)
    {
        try
        {
            work();
        }
        catch (Exception ex)
        {
            log.Write($"A PrismaUI callback threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string Text(byte* utf8) => Marshal.PtrToStringUTF8((nint)utf8) ?? string.Empty;

    private sealed class Callback(ulong handle, Action<string> handler, ILog log)
    {
        internal ulong Handle { get; } = handle;

        internal Action<string> Handler { get; } = handler;

        internal ILog Log { get; } = log;
    }
}
