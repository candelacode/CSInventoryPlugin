## Why

The existing BotManager.Plugin provides REST API endpoints for managing bot enable/disable state, but the project is being repurposed. We need an ASF plugin that monitors bot inventories for Counter Strike items and sends trade notifications to a main/master account. The plugin must be configurable per bot with a `sendcsitems` property.

## What Changes

- **Refactor project structure**: Rename solution, project folders, namespaces, and assembly name from `BotManagerPlugin` to `CSInventoryPlugin`
- **Remove Bot Management API**: Remove all REST API endpoints (Enable, Disable, Status) and the `BotManagementService`
- **Add CS inventory monitoring**: Listen for ASF trade/bot events, check inventory for CS items
- **Add trade notification system**: Send trade notifications (via ASF's own trading mechanisms) to a configured master account
- **Add per-bot configuration**: Support `sendcsitems` property (default true) to control per-bot CS item forwarding
- **Update specs**: Remove `bot-management` spec, update `github-autoupdate` spec with new repo name

## Capabilities

### New Capabilities
- `cs-item-monitor`: Monitor bot inventories for CS items and trigger trade notifications
- `trade-notification`: Send trade offers with CS items to the main/master account
- `cs-inventory-config`: Per-bot `sendcsitems` configuration property

### Modified Capabilities
- `github-autoupdate`: Update `RepositoryName` from `BotManagerPlugin` to `CSInventoryPlugin`

## Impact

- **BREAKING**: `BotManagerPlugin` assembly renamed to `CSInventoryPlugin` — breaking change for any existing installations
- **BREAKING**: All `/Api/BotManager/*` REST endpoints removed
- **New dependencies**: None beyond what ASF already provides (no external NuGet packages needed)
- **Config**: Each bot's `.json` config now supports `sendcsitems` (bool, default true)
- **Files renamed**: `BotManagementPlugin.cs` → `CSInventoryPlugin.cs`, all source files renamed and refactored
