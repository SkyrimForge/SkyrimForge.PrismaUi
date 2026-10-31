using SkyrimForge.Hosting;

namespace SkyrimForge.PrismaUi;

/// <summary>Adds <see cref="PrismaUiApi"/> to every plugin's services.</summary>
public sealed class PrismaUiServiceSource : IPluginServiceSource
{
    /// <inheritdoc/>
    public void AddServices(IServiceRegistrar services) => services.Add<PrismaUiApi>();
}
