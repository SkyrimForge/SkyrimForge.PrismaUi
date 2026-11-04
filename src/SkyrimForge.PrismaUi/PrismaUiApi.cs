using SkyrimForge.Events;
using System;

namespace SkyrimForge.PrismaUi;

/// <summary>The Prisma UI plugin, which draws HTML over the game.</summary>
/// <remarks>Callbacks run as queued work, so the rules on <see cref="ITaskQueue"/> apply. An exception a callback throws is logged.</remarks>
public sealed class PrismaUiApi
{
    private readonly ILog log;

    /// <summary>Requests Prisma UI's interface.</summary>
    /// <param name="log">Receives callback exceptions and the warnings and errors of each page.</param>
    /// <exception cref="InvalidOperationException">Prisma UI is not usable. The message states why.</exception>
    public PrismaUiApi(ILog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        this.log = log;
        ConsoleMessagesSupported = PrismaViews.Acquire();
    }

    /// <summary>Whether the installed Prisma UI raises <see cref="PrismaView.OnConsoleMessage"/>.</summary>
    public bool ConsoleMessagesSupported { get; }

    /// <summary>Whether any view in the process has focus, including views other mods created.</summary>
    public static bool HasAnyActiveFocus => NativeBindings.SFP_HasAnyActiveFocus();

    /// <summary>Creates a view over a page.</summary>
    /// <param name="htmlPath">A path under <c>Data\PrismaUI\views</c>, such as <c>mymod/index.html</c>, or an <c>http://</c> or <c>https://</c> URL.</param>
    /// <returns>The view.</returns>
    /// <remarks>
    /// Call this from <see cref="IGameEvents.OnInputLoaded"/> or later, because Prisma UI initializes on the first view and requires the input device manager.
    /// A missing page still returns a view, and DOM ready is never raised. The process ends if Prisma UI's renderer fails to start.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="htmlPath"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Prisma UI created no view.</exception>
    public PrismaView CreateView(string htmlPath) =>
        CreateViewOrNull(htmlPath) ?? throw new InvalidOperationException($"Prisma UI created no view for {htmlPath}.");

    /// <summary>Creates a view over a page, or returns <c>null</c> if Prisma UI creates none.</summary>
    /// <inheritdoc cref="CreateView(string)" path="/param"/>
    /// <returns>The view, or <c>null</c> if Prisma UI created none.</returns>
    /// <inheritdoc cref="CreateView(string)" path="/remarks"/>
    /// <exception cref="ArgumentNullException"><paramref name="htmlPath"/> is <c>null</c>.</exception>
    public PrismaView? CreateViewOrNull(string htmlPath)
    {
        ArgumentNullException.ThrowIfNull(htmlPath);

        return PrismaViews.CreateView(htmlPath, log);
    }
}
