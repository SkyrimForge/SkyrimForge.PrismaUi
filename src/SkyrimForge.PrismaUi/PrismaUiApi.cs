using System;

namespace SkyrimForge.PrismaUi;

/// <summary>The PrismaUI plugin, which draws HTML over the game.</summary>
/// <remarks>Callbacks run as queued work, so the rules on <see cref="ITaskQueue"/> apply. An exception a callback throws is logged.</remarks>
public sealed class PrismaUiApi
{
    private readonly ILog log;

    /// <summary>Requests PrismaUI's interface.</summary>
    /// <param name="log">Receives callback exceptions and the warnings and errors of each page.</param>
    /// <exception cref="InvalidOperationException">PrismaUI is not usable. The message states why.</exception>
    public PrismaUiApi(ILog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        this.log = log;
        PrismaUiAvailability availability = PrismaViews.Acquire();
        ConsoleMessagesSupported = availability switch
        {
            PrismaUiAvailability.Available => true,
            PrismaUiAvailability.AvailableWithoutConsoleMessages => false,
            _ => throw new InvalidOperationException(Explain(availability))
        };
    }

    private static string Explain(PrismaUiAvailability availability) => availability switch
    {
        PrismaUiAvailability.PluginNotLoaded => "PrismaUI.dll is not loaded. Install or enable PrismaUI.",
        PrismaUiAvailability.PluginHasNoEntryPoint => "PrismaUI.dll exports no RequestPluginAPI.",
        PrismaUiAvailability.PluginReturnedNoInterface => "PrismaUI returned no interface.",
        _ => "SkyrimForge.PrismaUi.dll is loaded more than once. "
            + "Deploy it only in SKSE\\Plugins\\SkyrimForge\\SharedLibraries\\SkyrimForge.PrismaUi, not beside the plugins."
    };

    /// <summary>Whether the installed PrismaUI raises <see cref="PrismaView.OnConsoleMessage"/>.</summary>
    public bool ConsoleMessagesSupported { get; }

    /// <summary>Whether any view in the process has focus, including views other mods created.</summary>
    public static bool HasAnyActiveFocus => NativeBindings.SFP_HasAnyActiveFocus();

    /// <summary>Creates a view over a page.</summary>
    /// <param name="htmlPath">A path under <c>Data\PrismaUI\views</c>, such as <c>mymod/index.html</c>, or an <c>http://</c> or <c>https://</c> URL.</param>
    /// <returns>The view, or <c>null</c> if PrismaUI created none.</returns>
    /// <remarks>
    /// Call this from <see cref="IGameEvents.OnInputLoaded"/> or later, because PrismaUI initializes on the first view and requires the input device manager.
    /// A missing page still returns a view, and DOM ready is never raised. The process ends if PrismaUI's renderer fails to start.
    /// </remarks>
    public PrismaView? CreateViewOrNull(string htmlPath)
    {
        ArgumentNullException.ThrowIfNull(htmlPath);

        return PrismaViews.CreateView(htmlPath, log);
    }
}
