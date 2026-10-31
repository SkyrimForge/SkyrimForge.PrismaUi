using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyrimForge.PrismaUi;

/// <summary>A PrismaUI view: a page drawn over the game.</summary>
/// <remarks>After <see cref="Destroy"/>, members have no effect.</remarks>
public sealed class PrismaView
{
    internal PrismaView(ulong handle, ILog log)
    {
        Handle = handle;
        Log = log;
        DomReady = new GameEvent<DomReadyEventArgs>(LogException);
        ConsoleMessage = new GameEvent<ConsoleMessageEventArgs>(LogException);
    }

    internal ulong Handle { get; private set; }

    internal ILog Log { get; }

    internal GameEvent<DomReadyEventArgs> DomReady { get; }

    internal GameEvent<ConsoleMessageEventArgs> ConsoleMessage { get; }

    internal Dictionary<string, uint> ListenerSlots { get; } = [];

    /// <summary>Raised once the page's DOM is built. A later subscriber receives nothing.</summary>
    public IGameEvent<DomReadyEventArgs> OnDomReady => DomReady;

    /// <summary>Raised for each message the page writes to the console. Warnings and errors are logged regardless.</summary>
    public IGameEvent<ConsoleMessageEventArgs> OnConsoleMessage => ConsoleMessage;

    private void LogException(Exception ex) => Log.Write($"A PrismaUI event subscriber threw and was dropped: {ex.GetType().Name}: {ex.Message}");

    /// <summary>Whether PrismaUI still has this view.</summary>
    public bool IsValid => Handle != 0 && NativeBindings.SFP_IsValid(Handle);

    /// <summary>Whether this view has input focus.</summary>
    public bool HasFocus => Handle != 0 && NativeBindings.SFP_HasFocus(Handle);

    /// <summary>Whether this view is hidden.</summary>
    public bool IsHidden => Handle == 0 || NativeBindings.SFP_IsHidden(Handle);

    /// <summary>Pixels scrolled per wheel notch.</summary>
    public int ScrollingPixelSize
    {
        get => Handle != 0 ? NativeBindings.SFP_GetScrollingPixelSize(Handle) : 0;
        set
        {
            if (Handle != 0)
            {
                NativeBindings.SFP_SetScrollingPixelSize(Handle, value);
            }
        }
    }

    /// <summary>This view's place in the draw order. Higher draws over lower.</summary>
    public int Order
    {
        get => Handle != 0 ? NativeBindings.SFP_GetOrder(Handle) : -1;
        set
        {
            if (Handle != 0)
            {
                NativeBindings.SFP_SetOrder(Handle, value);
            }
        }
    }

    /// <summary>Whether the inspector is visible. Call <see cref="CreateInspectorView"/> first.</summary>
    public bool IsInspectorVisible
    {
        get => Handle != 0 && NativeBindings.SFP_IsInspectorVisible(Handle);
        set
        {
            if (Handle != 0)
            {
                NativeBindings.SFP_SetInspectorVisibility(Handle, value);
            }
        }
    }

    /// <summary>Evaluates JavaScript and returns the result as text, or an empty string if the script throws or does not parse.</summary>
    /// <remarks>Await this. Blocking on it on the game's thread or in queued work can deadlock, because the result arrives as queued work.</remarks>
    public Task<string> Invoke(string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        return Handle != 0 ? PrismaViews.Invoke(Handle, script, Log) : Task.FromResult(string.Empty);
    }

    /// <summary>Calls a function the page declares, passing one string.</summary>
    public void InteropCall(string functionName, string argument)
    {
        ArgumentNullException.ThrowIfNull(functionName);
        ArgumentNullException.ThrowIfNull(argument);

        if (Handle != 0)
        {
            NativeBindings.SFP_InteropCall(Handle, functionName, argument);
        }
    }

    /// <summary>Registers a global function the page can call. Registering a name again replaces its handler.</summary>
    /// <returns>A subscription that stops the handler, or <c>null</c> if the view is gone or no slot is free.</returns>
    /// <remarks>PrismaUI binds the name after the page finishes loading, which can follow <see cref="OnDomReady"/>.</remarks>
    public ISubscription? RegisterJSListener(string functionName, Action<string> handler)
    {
        ArgumentNullException.ThrowIfNull(functionName);
        ArgumentNullException.ThrowIfNull(handler);

        if (IsValid)
        {
            return PrismaViews.RegisterListener(this, functionName, handler);
        }

        Log.Write($"{nameof(RegisterJSListener)} did nothing for \"{functionName}\", because PrismaUI has no such view.");
        return null;
    }

    /// <summary>Gives this view input focus.</summary>
    /// <returns><c>false</c> if the view is gone. The work is queued, so <see cref="HasFocus"/> can still be <c>false</c> on return.</returns>
    /// <remarks>
    /// A hidden view cannot take focus. Outside Skyrim VR, this disables the player's controls until <see cref="Unfocus"/>, and <paramref name="pauseGame"/> pauses the game.
    /// <paramref name="disableFocusMenu"/> omits PrismaUI's focus menu, which draws the cursor. Outside Skyrim VR, typing into a page requires the Media Keys Fix mod.
    /// </remarks>
    public bool Focus(bool pauseGame = false, bool disableFocusMenu = false) => Handle != 0 && NativeBindings.SFP_Focus(Handle, pauseGame, disableFocusMenu);

    /// <summary>Removes input focus and restores the player's controls.</summary>
    public void Unfocus()
    {
        if (Handle != 0)
        {
            NativeBindings.SFP_Unfocus(Handle);
        }
    }

    /// <summary>Draws this view again.</summary>
    public void Show()
    {
        if (Handle != 0)
        {
            NativeBindings.SFP_Show(Handle);
        }
    }

    /// <summary>Stops drawing this view without destroying it.</summary>
    public void Hide()
    {
        if (Handle != 0)
        {
            NativeBindings.SFP_Hide(Handle);
        }
    }

    /// <summary>Creates the inspector, which requires the Ultralight SDK's inspector folder at <c>Data\PrismaUI\inspector</c>.</summary>
    public void CreateInspectorView()
    {
        if (Handle != 0)
        {
            NativeBindings.SFP_CreateInspectorView(Handle);
        }
    }

    /// <summary>Positions and sizes the inspector in screen pixels.</summary>
    public void SetInspectorBounds(float topLeftX, float topLeftY, uint width, uint height)
    {
        if (Handle != 0)
        {
            NativeBindings.SFP_SetInspectorBounds(Handle, topLeftX, topLeftY, width, height);
        }
    }

    /// <summary>Destroys the view, blocking until PrismaUI has released it. A view otherwise survives a save load.</summary>
    public void Destroy()
    {
        ulong handle = Handle;
        if (handle != 0)
        {
            PrismaViews.Remove(this);
            Handle = 0;
            NativeBindings.SFP_Destroy(handle);
        }
    }
}
