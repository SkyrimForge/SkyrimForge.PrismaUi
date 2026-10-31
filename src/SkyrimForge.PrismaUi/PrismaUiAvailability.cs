namespace SkyrimForge.PrismaUi;

internal enum PrismaUiAvailability
{
    Available,
    AvailableWithoutConsoleMessages,
    PluginNotLoaded,
    PluginHasNoEntryPoint,
    PluginReturnedNoInterface,
    LoadedMoreThanOnce
}
