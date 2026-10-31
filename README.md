# SkyrimForge.PrismaUi

SkyrimForge.PrismaUi lets a [SkyrimForge](https://github.com/SkyrimForge/SkyrimForge) plugin create and script
[PrismaUI](https://github.com/PrismaUI-SKSE/framework) views in C#. PrismaUI renders HTML, CSS, and JavaScript over the game.

## Requirements to Run

- [SkyrimForge](https://www.nexusmods.com/skyrimspecialedition/mods/196701)
- [PrismaUI](https://www.nexusmods.com/skyrimspecialedition/mods/148718)
- [SkyrimForge.PrismaUi](https://www.nexusmods.com/skyrimspecialedition/mods/197581), which contains the binding and its native shim
- [Media Keys Fix](https://www.nexusmods.com/skyrimspecialedition/mods/92948), only for text input in a page, except on Skyrim VR

Without PrismaUI, the `PrismaUiApi` constructor throws an `InvalidOperationException` whose message states the cause.

## Writing a Plugin

A plugin references the `SkyrimForge.PrismaUi` NuGet package and declares `PrismaUiApi` as a constructor parameter:

```csharp
using SkyrimForge;
using SkyrimForge.PrismaUi;

namespace ViewPlugin;

public sealed class ViewPlugin(PrismaUiApi prisma, IGameEvents events) : IPlugin
{
    public void Start()
    {
        events.OnInputLoaded.Subscribe(_ => prisma.CreateViewOrNull("view-plugin/index.html"));
    }
}
```

Create a view from `OnInputLoaded` or later. The path is relative to `Data\PrismaUI\views`.

The `SkyrimForge.PrismaUi.PluginTemplate` package creates a plugin that scripts a view, receives calls from its JavaScript, and destroys it:

```powershell
dotnet new install SkyrimForge.PrismaUi.PluginTemplate
dotnet new skyrimforge-prismaui-plugin --name <YourPluginName>
```

## Documentation

[docs.skyrimforge.net/prismaui](https://docs.skyrimforge.net/prismaui/) covers views and the API reference.

## Building

Building requires the .NET 10 SDK and Visual Studio's "Desktop development with C++" workload.

```powershell
.\Build.ps1
```

`Deploy.ps1` builds SkyrimForge.PrismaUi and copies it into a Mod Organizer 2 mod folder. Add `-NoBuild` to copy the last build.

## License

SkyrimForge.PrismaUi is licensed under the [GNU General Public License, version 3 or later](https://github.com/SkyrimForge/SkyrimForge.PrismaUi/blob/master/LICENSE), with the additional permissions in
[EXCEPTIONS.md](https://github.com/SkyrimForge/SkyrimForge.PrismaUi/blob/master/EXCEPTIONS.md), which permit linking it with Skyrim, SKSE, and PrismaUI. A plugin built on it is a combined work with it,
so its license must be compatible with the GPL. See [NOTICE](https://github.com/SkyrimForge/SkyrimForge.PrismaUi/blob/master/NOTICE) and [THIRD-PARTY-NOTICES](https://github.com/SkyrimForge/SkyrimForge.PrismaUi/blob/master/THIRD-PARTY-NOTICES) for the notices that copies must retain.
