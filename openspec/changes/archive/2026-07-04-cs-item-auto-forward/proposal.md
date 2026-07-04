## Why

When CSInventoryPlugin is loaded by ASF, it automatically monitors incoming trade results for Counter-Strike (appId 730) items. If CS items are detected, the plugin forwards them to the bot's master account via a trade offer. This eliminates the need for manual item transfer and ensures CS items are consolidated to the master account automatically.

## What Changes

- Monitor all `OnBotTradeOfferResults` callbacks from ASF for CS items
- Check per-bot `sendcsitems` config property (boolean, default true) to allow opt-out per bot
- When CS items are found, send a trade offer containing all detected CS items to the bot's master account
- Skip forwarding if the master account is the bot itself (prevent self-trading)
- Log warnings when no master is configured, self-trade detected, or trade sending fails
- Read `sendcsitems` via `IBotModules.OnBotInitModules` API instead of accessing `BotConfig.AdditionalProperties` directly (which is `internal`)

## Capabilities

### New Capabilities

<!-- No new capabilities - detection and forwarding behavior already specified in existing specs -->

### Modified Capabilities

- `cs-inventory-config`: Updated to read `sendcsitems` via `IBotModules` plugin API instead of direct `internal` property access
- `trade-notification`: Updated logging requirement to reflect actual logged fields (item count, bot name, target master)

## Impact

- `CSInventoryPlugin.cs` - main plugin class
- Implements `IBotModules` interface for receiving per-bot config properties
- PostBuild target copies plugin DLL to ASF's plugins output directory
- ASF `BotConfig.AdditionalProperties` is `internal` - must use `IBotModules.OnBotInitModules` API instead
