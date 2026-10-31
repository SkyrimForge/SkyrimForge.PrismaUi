namespace SkyrimForge.PrismaUi;

/// <summary>A message a page wrote to the JavaScript console.</summary>
public readonly record struct ConsoleMessageEventArgs(ConsoleMessageLevel Level, string Text);
