# CSInventory.Plugin

---

## Description

CSInventory.Plugin is a plugin for [ArchiSteamFarm](https://github.com/JustArchiNET/ArchiSteamFarm) that monitors bot inventories for Counter Strike (appId 730) items and sends trade notifications to the configured master account.

### Features

- **CS Item Detection**: Automatically detects CS items in bot inventories after trade offers
- **Trade Notification**: Sends detected CS items to the master account via ASF trade offers
- **Per-Bot Configuration**: Control CS item forwarding per bot with `SendCSItems` property
- **Zero Configuration**: Works out of the box with sensible defaults

---

## How to use this plugin

### Installation

1. Download the latest release from [Releases](https://github.com/candelacode/CSInventoryPlugin/releases)
2. Extract the contents to ASF's `plugins/CSInventoryPlugin/` directory
3. Restart ASF

### Configuration

The plugin supports a per-bot `SendCSItems` property in each bot's JSON configuration:

```json
{
  "SendCSItems": true
}
```

- `SendCSItems` (bool, optional, default: `true`): Set to `false` to disable CS item trade notifications for a specific bot

When a bot receives CS items in a trade, the plugin will automatically forward those items to the bot's configured master account via a new trade offer.

---

## Building from source

### Prerequisites

- .NET 10.0 SDK or later
- Git

### Build

1. Clone the repository:
   ```bash
   git clone --recursive https://github.com/candelacode/CSInventoryPlugin.git
   cd CSInventory.Plugin
   ```

2. Build the plugin:
   ```bash
   dotnet build -c Release
   ```

3. Or use the build script:
   ```bash
   build.bat
   ```

The compiled plugin will be in `CSInventoryPlugin/bin/Release/net10.0/CSInventoryPlugin.dll`.

### Project Structure

```
CSInventory.Plugin/
├── CSInventoryPlugin/              # Plugin source
│   ├── CSInventoryPlugin.cs        # Plugin entry point (ASF lifecycle + trade monitoring)
│   └── CSInventoryPlugin.csproj
├── CSInventoryPlugin.Tests/        # Unit tests
│   ├── CSInventoryPluginTests.cs
│   └── CSInventoryPlugin.Tests.csproj
├── ArchiSteamFarm/                 # Git submodule (ASF source)
├── Directory.Build.props
├── Directory.Packages.props
└── CSInventoryPlugin.slnx
```

---

## How it works

The plugin implements the `IBotTradeOfferResults` ASF plugin interface. When a bot processes a trade offer:

1. The plugin is notified via `OnBotTradeOfferResults()`
2. It refreshes the bot's inventory cache
3. It checks for items with `appId == 730` (Counter Strike)
4. If CS items are found and the bot's `SendCSItems` config is not `false`, it creates a trade offer to the master account containing those items

---

## Plugin development

This section summarizes how ASF plugins are built. For the full guide, see the [ASF Plugins development wiki](https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Plugins-development).

### Project structure

An ASF plugin is a standard .NET library targeting the same .NET framework as the target ASF version (e.g. `net10.0`). The `.csproj` must reference the main `ArchiSteamFarm` assembly and `System.Composition.AttributedModel` at minimum:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- ASF already includes these; IncludeAssets="compile" avoids bundling them -->
    <PackageReference Include="System.Composition.AttributedModel" IncludeAssets="compile" />
    <PackageReference Include="SteamKit2" IncludeAssets="compile" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference ASF source with ExcludeAssets="all" so no ASF files are produced -->
    <ProjectReference Include="..\ArchiSteamFarm\ArchiSteamFarm\ArchiSteamFarm.csproj" ExcludeAssets="all" Private="false" />
    <!-- Or reference a prebuilt DLL: -->
    <!-- <Reference Include="ArchiSteamFarm" HintPath="path\to\ArchiSteamFarm.dll" /> -->
  </ItemGroup>
</Project>
```

### IPlugin interface and export

A plugin class must inherit from `IPlugin` (or a more specialized interface) and be annotated with `[Export(typeof(IPlugin))]` so ASF can discover and load it via `System.Composition`:

```csharp
using System.Composition;
using System.Threading.Tasks;
using ArchiSteamFarm;
using ArchiSteamFarm.Plugins;

namespace MyPlugin;

[Export(typeof(IPlugin))]
public sealed class MyPlugin : IPlugin {
	public string Name => nameof(MyPlugin);
	public Version Version => typeof(MyPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo("Hello World!");
		return Task.CompletedTask;
	}
}
```

### Common plugin interfaces

ASF exposes several interfaces in `ArchiSteamFarm.Plugins.Interfaces`:

| Interface | Purpose |
|---|---|
| `IASF` | ASF initialization (`OnASFInit`) |
| `IBot` | Bot lifecycle (`OnBotInit`, `OnBotDestroy`) |
| `IBotModules` | Per-bot config properties (`OnBotInitModules`) |
| `IBotConnection` | Bot connection events (`OnBotLoggedOn`, `OnBotDisconnected`) |
| `IBotTradeOfferResults` | Trade offer processing results (`OnBotTradeOfferResults`) |
| `IBotSteamClient` | Custom SteamKit2 handlers |
| `IBotCommand2` | Custom commands |
| `IGitHubPluginUpdates` | GitHub-based automatic plugin updates |
| `IPluginUpdates` | Custom update mechanism |

See the `ArchiSteamFarm.Plugins.Interfaces` namespace and the [ExamplePlugin](https://github.com/JustArchiNET/ArchiSteamFarm/tree/main/ArchiSteamFarm/Plugins/ExamplePlugin) for full examples.

### Shared dependency handling

Dependencies already included in ASF (e.g. `ArchiSteamFarm`, `SteamKit2`, `AngleSharp`) should be marked with `IncludeAssets="compile"` to avoid bundling them in the plugin output. This reduces memory footprint and plugin size. Only include libraries that ASF does not ship (e.g. `Discord.Net`).

### Native dependencies caveat

ASF OS-specific builds trim the .NET runtime to reduce size. If your plugin uses .NET features not covered by the trimmed runtime, you may encounter `System.MissingMethodException` or `System.Reflection.ReflectionTypeLoadException`. Verify your plugin against the ASF **generic** build first; if it works there but fails on an OS-specific build, it's a native dependency issue. Running custom plugins in the generic ASF flavor is recommended.

### Automatic updates

ASF provides two update interfaces:

- **`IGitHubPluginUpdates`**: GitHub-based updates. Set `RepositoryName`, use version-parsable tags (e.g. `v1.0.0.0`), ensure the plugin's `Version` matches the tag, and attach a `.zip` release asset with the plugin DLL at the root.
- **`IPluginUpdates`**: Custom update mechanism. Override `GetTargetReleaseURL()` to return the update URL. Supports `OnPluginUpdateProceeding()` and `OnPluginUpdateFinished()` hooks.

Both require appropriate ASF config values (`PluginsUpdateMode`, `PluginsUpdateList`).

### Receiving automatic updates (user-side)

The plugin implements `IGitHubPluginUpdates` with `RepositoryName = "candelacode/CSInventoryPlugin"`. To allow ASF to auto-update this plugin, set the following in your ASF global config (`globalConfig.json`):

```json
{
  "PluginsUpdateMode": 3,
  "PluginsUpdateList": {
    "CSInventoryPlugin": true
  }
}
```

- `PluginsUpdateMode: 3` — update only explicitly-listed plugins.
- `PluginsUpdateList.CSInventoryPlugin: true` — opt this plugin into auto-updates.

When a new `vX.Y.Z.W` release is published on GitHub, ASF will download `CSInventoryPlugin.zip` from the latest release, replace the plugin's binaries in `plugins/CSInventoryPlugin/`, and restart.

---

## Releasing (maintainer-side)

To cut a new release of this plugin:

1. Bump `<Version>` in `Directory.Build.props` to the new 4-part value (e.g. `1.0.1.0`). The version MUST be 4 parts (`Major.Minor.Build.Revision`) and parsable as a `System.Version`. The 3-part form `1.0.1` is not accepted.
2. Commit the change on `main`:
   ```bash
   git add Directory.Build.props
   git commit -m "Bump version to 1.0.1.0"
   git push origin main
   ```
3. Create and push the matching `vX.Y.Z.W` tag on the merge commit:
   ```bash
   git tag v1.0.1.0
   git push origin v1.0.1.0
   ```
4. The `.github/workflows/publish.yml` `release` job runs automatically. It builds the plugin on `ubuntu-latest`, `macos-latest`, and `windows-latest`, generates SHA512SUMS, and publishes a non-prerelease GitHub release with `CSInventoryPlugin.zip` (plus the per-OS zips) attached.

The tag body (the part after `v`) MUST match `<Version>` exactly. If they differ, the build will still succeed but the resulting DLL's version will not match the tag and ASF will not auto-update users.

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE.txt](LICENSE.txt) file for details.

---

## Acknowledgments

- [ArchiSteamFarm](https://github.com/JustArchiNET/ArchiSteamFarm) - The amazing Steam bot framework
- [ASF-PluginTemplate](https://github.com/JustArchiNET/ASF-PluginTemplate) - Template for ASF plugins
