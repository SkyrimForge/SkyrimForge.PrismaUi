# SkyrimForge.PrismaUi.PluginTemplate

The `dotnet new` template for a SkyrimForge plugin with a [PrismaUI](https://github.com/PrismaUI-SKSE/framework) view: a .NET 10 class library that references
the [SkyrimForge.PrismaUi](https://www.nuget.org/packages/SkyrimForge.PrismaUi) package, with a plugin class and a page that call each other.

```powershell
dotnet new install SkyrimForge.PrismaUi.PluginTemplate
dotnet new skyrimforge-prismaui-plugin --name <YourPluginName>
```

The project, its plugin class, and the page's folder take the name `--name` gives. Choose a name unique to your mod:
by default, the plugin class's full name is its identity, from which its log file and its saved state take their names.

## Documentation

[docs.skyrimforge.net/prismaui](https://docs.skyrimforge.net/prismaui/) covers views and the API reference.

## License

SkyrimForge.PrismaUi is licensed under the GNU General Public License, version 3 or later, with the additional permissions in its
[EXCEPTIONS.md](https://github.com/SkyrimForge/SkyrimForge.PrismaUi/blob/master/EXCEPTIONS.md), which permit linking it with Skyrim, SKSE, and PrismaUI.
A plugin built on it is a combined work with it, so its license must be compatible with the GPL.
