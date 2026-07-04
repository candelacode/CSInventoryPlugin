## Why

The plugin currently only detects CS items reactively, after a trade is processed via `OnBotTradeOfferResults`. Items that arrive through other paths (e.g. grants, market listings, off-ASF trades, or items already present when the bot starts) are never forwarded to the master account. Additionally, the repository lacks developer-facing documentation explaining how ASF plugins are built, making contribution and maintenance harder.

## What Changes

- Add a bot-startup inventory scan: when a bot finishes initialization and is connected/logged on, the plugin refreshes that bot's inventory and forwards any detected CS (appId 730) items to the configured master account using the existing trade-notification flow.
- Respect the existing per-bot `sendcsitems` config flag so the startup scan can be disabled per bot.
- Generalize the trade-notification trigger wording so it applies to CS items detected by any mechanism (trade results OR startup scan), not only "after a trade".
- Add an `asf-plugin-development` documentation spec capturing the conventions and requirements for building ArchiSteamFarm plugins (project layout, `IPlugin` interfaces, `[Export]`, dependencies, native-deps caveats, and GitHub-based auto-updates) so contributors have a single reference.
- Add a developer documentation section to the README summarizing ASF plugin development.

## Capabilities

### New Capabilities
- `bot-startup-cs-scan`: Scans a bot's inventory for CS (appId 730) items when the bot starts and is connected, then forwards detected items to the master account.
- `asf-plugin-development`: Documents the conventions, project structure, and APIs required to develop ArchiSteamFarm plugins, serving as a contributor reference.

### Modified Capabilities
- `trade-notification`: Generalize the detection trigger so notifications apply to CS items detected by any mechanism (startup scan or trade results), not only items detected "after a trade".

## Impact

- **Code**: `CSInventoryPlugin/CSInventoryPlugin.cs` gains startup-scan logic and likely a new ASF plugin interface implementation (e.g. `IBotSteamClient` or `IASF`-initiated scan) to trigger inventory refresh + `SendInventory` on bot ready.
- **Specs**: New `bot-startup-cs-scan` and `asf-plugin-development` specs; delta spec for `trade-notification`.
- **Docs**: `README.md` gains a developer documentation section summarizing ASF plugin development.
- **Dependencies**: No new external dependencies; reuses existing ASF APIs (`Bot.ArchiWebHandler`, `Bot.Actions.SendInventory`).
- **Compatibility**: No breaking changes; the startup scan defaults to enabled and respects `sendcsitems`.
