## Why

The per-bot config property `sendcsitems` uses all-lowercase naming, which is inconsistent with ASF's PascalCase config convention (e.g., `SteamLogin`, `SteamMasterID`, `Enabled`). Users familiar with ASF config expect PascalCase keys, so the current name is a minor but real friction point and a documentation inconsistency. Aligning the key to `SendCSItems` makes the plugin's config feel native to ASF.

## What Changes

- **BREAKING**: The canonical config property name changes from `sendcsitems` to `SendCSItems`.
- The config parser (`CSBotConfig`) SHALL accept `SendCSItems` as the canonical key.
- For backward compatibility, the parser SHALL also accept the legacy `sendcsitems` key and log a deprecation warning when the legacy key is the one present, so existing user configs keep working until they migrate.
- README documentation and in-code log messages SHALL reference `SendCSItems`.
- Unit tests SHALL cover both the new canonical key and the legacy fallback (including the deprecation warning path).

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `cs-inventory-config`: The per-bot config property is renamed from `sendcsitems` to `SendCSItems`, with legacy fallback and a deprecation warning for the old key.
- `trade-notification`: Scenario references to the `sendcsitems` config flag are updated to `SendCSItems` (with legacy fallback behavior).
- `bot-startup-cs-scan`: Scenario references to the `sendcsitems` config flag are updated to `SendCSItems` (with legacy fallback behavior).
- `plugin-code-organization`: `CSBotConfig.TryGetSendCsItems()` parsing behavior is updated to look up `SendCSItems` first, fall back to `sendcsitems`, and report which key was used so the caller can log a deprecation warning.

## Impact

- **Code**: `CSInventoryPlugin/CSBotConfig.cs` (lookup key + legacy fallback + which-key reporting), `CSInventoryPlugin/CSInventoryPlugin.cs` (log messages reference `SendCSItems`; deprecation warning emitted when legacy key used).
- **Docs**: `README.md` (config example, property description, and how-it-works section).
- **Tests**: `CSInventoryPlugin.Tests/CSInventoryPluginTests.cs` (existing `sendcsitems` test cases updated to `SendCSItems`; new cases for legacy fallback and deprecation warning).
- **User config**: Existing bots configured with `"sendcsitems": false` continue to work (legacy fallback) but emit a deprecation warning on startup/logon until the user migrates to `SendCSItems`.
- **No public API change**: `CSBotConfig.TryGetSendCsItems()` signature stays the same; only the lookup behavior and the validity/which-key reporting change.
