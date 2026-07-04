# CSInventory.Plugin

---

## Description

CSInventory.Plugin is a plugin for [ArchiSteamFarm](https://github.com/JustArchiNET/ArchiSteamFarm) that monitors bot inventories for Counter Strike (appId 730) items and sends trade notifications to the configured master account.

### Features

- **CS Item Detection**: Automatically detects CS items in bot inventories after trade offers
- **Trade Notification**: Sends detected CS items to the master account via ASF trade offers
- **Per-Bot Configuration**: Control CS item forwarding per bot with `sendcsitems` property
- **Zero Configuration**: Works out of the box with sensible defaults

---

## How to use this plugin

### Installation

1. Download the latest release from [Releases](https://github.com/candelacode/CSInventoryPlugin/releases)
2. Extract the contents to ASF's `plugins/CSInventoryPlugin/` directory
3. Restart ASF

### Configuration

The plugin supports a per-bot `sendcsitems` property in each bot's JSON configuration:

```json
{
  "sendcsitems": true
}
```

- `sendcsitems` (bool, optional, default: `true`): Set to `false` to disable CS item trade notifications for a specific bot

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
4. If CS items are found and the bot's `sendcsitems` config is not `false`, it creates a trade offer to the master account containing those items

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
