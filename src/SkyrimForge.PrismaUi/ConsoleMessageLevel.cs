namespace SkyrimForge.PrismaUi;

/// <summary>The severity of a JavaScript console message.</summary>
public enum ConsoleMessageLevel : byte
{
    /// <summary><c>console.log</c>.</summary>
    Log = 0,

    /// <summary><c>console.warn</c>.</summary>
    Warning = 1,

    /// <summary><c>console.error</c>, and uncaught exceptions.</summary>
    Error = 2,

    /// <summary><c>console.debug</c>.</summary>
    Debug = 3,

    /// <summary><c>console.info</c>.</summary>
    Info = 4
}
