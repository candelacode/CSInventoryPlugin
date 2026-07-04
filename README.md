# CSInventory.Plugin

A plugin for [ArchiSteamFarm](https://github.com/JustArchiNET/ArchiSteamFarm) that monitors bot inventories for Counter Strike (appId 730) items and forwards them to the configured master account via trade offers.

## Features

- **CS item detection** in bot inventories after trade offers
- **Startup scan** of bot CS inventory when the bot logs on
- **Per-bot opt-in** via the `SendCSItems` JSON property (default: `false`)
- **Explicit state logging** so you can confirm forwarding is on or off at a glance

## Installation

1. Download the latest release from [Releases](https://github.com/candelacode/CSInventoryPlugin/releases)
2. Extract the contents to ASF's `plugins/CSInventoryPlugin/` directory
3. Restart ASF

## Configuration

Add the per-bot `SendCSItems` property to each bot's JSON configuration:

```json
{
  "SendCSItems": true
}
```

- `SendCSItems` (bool, optional, **default: `false`**): set to `true` to forward CS items from that bot to its master account. When the property is absent, forwarding is off and the plugin stays silent about the config. When the property is explicitly set, the plugin logs `SendCSItems is enabled.` or `SendCSItems is disabled.` once per relevant event so you can confirm the effective state.

> **Upgrading from an earlier version?** The default flipped from `true` to `false`. Set `"SendCSItems": true` in any bot that should keep forwarding.

## How it works

When CS items appear in a bot's inventory, the plugin sends a trade offer containing those items to the bot's configured master account. Two paths cover the common cases:

- **Startup**: when a bot finishes logging on, the plugin does a one-time scan of its CS inventory and forwards any items found. Reconnects do not re-trigger the scan.
- **Trade results**: after a bot processes a trade offer, any CS items it received are forwarded.

If the bot's master is the bot itself, or no master is configured, the plugin logs a warning and skips the trade. If `SendInventory` fails, the plugin logs the error and does not retry.

## Automatic updates

To allow ASF to auto-update this plugin, add it to your ASF global config (`globalConfig.json`):

```json
{
  "PluginsUpdateMode": 3,
  "PluginsUpdateList": {
    "CSInventoryPlugin": true
  }
}
```

When a new `vX.Y.Z.W` release is published, ASF downloads `CSInventoryPlugin.zip` from the latest release, replaces the binaries in `plugins/CSInventoryPlugin/`, and restarts.

## Building

Requires the .NET 10.0 SDK and Git.

```bash
git clone --recursive https://github.com/candelacode/CSInventoryPlugin.git
cd CSInventory.Plugin
dotnet build -c Release
# or
build.bat
```

The compiled plugin is at `CSInventoryPlugin/bin/Release/net10.0/CSInventoryPlugin.dll`.

## Project layout

```
CSInventory.Plugin/
├── CSInventoryPlugin/          # Plugin source
├── CSInventoryPlugin.Tests/    # xUnit unit tests
├── ArchiSteamFarm/             # ASF source (git submodule)
├── Directory.Build.props
├── Directory.Packages.props
└── CSInventoryPlugin.slnx
```

For the per-file layout, plugin interface details, the release workflow, and contributor notes, see [architecture.md](architecture.md).

## Contributing

Contributions are welcome — please open a Pull Request.

## License

Apache License 2.0 — see [LICENSE.txt](LICENSE.txt).

## Acknowledgments

- [ArchiSteamFarm](https://github.com/JustArchiNET/ArchiSteamFarm)
- [ASF-PluginTemplate](https://github.com/JustArchiNET/ASF-PluginTemplate)
