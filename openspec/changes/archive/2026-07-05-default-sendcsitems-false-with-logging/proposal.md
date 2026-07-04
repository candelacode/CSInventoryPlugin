## Why

The `SendCSItems` per-bot config currently defaults to `true`, which means every bot that does not explicitly opt out will silently forward its CS items to the master account. That default is surprising for a plugin that is not core to ASF: most operators will install it expecting opt-in behavior, and a bot that was never told to forward will quietly start doing so on the next trade. We want forwarding to be an explicit choice. In addition, today the plugin only logs when `SendCSItems` is explicitly `false` ("skipped") — there is no positive confirmation that the feature is enabled and no quiet path for users who never set the property. Operators cannot tell at a glance whether forwarding is actually on for a given bot.

## What Changes

- **BREAKING**: The default value of the `SendCSItems` config property changes from `true` to `false`. Bots that do not set `"SendCSItems": true` in their JSON config will no longer forward CS items.
- The plugin SHALL log a single info line per bot, once per relevant event (startup / trade-result), that states the effective state of `SendCSItems`:
  - **Not set** (key absent or `additionalProperties == null`): no config-related log line is emitted. Forwarding is off by default and we stay silent, because there is nothing for the user to act on.
  - **Set to `true`**: log an info line indicating `SendCSItems` is enabled for the bot.
  - **Set to `false`**: log an info line indicating `SendCSItems` is disabled for the bot.
  - **Set to a non-boolean value**: keep the existing warning about the invalid value AND emit the disabled info line (because the parser falls back to the default, which is now `false`).
- `CSBotConfig.TryGetSendCsItems()` updates its default from `true` to `false` for all "no value" paths (null dictionary, missing key, non-boolean value).
- README documentation is updated to reflect the new default and the logging behavior.
- Unit tests are updated to assert the new default and the new "no log when unset" expectation where it is observable in unit tests (parser-level defaults; log-emission is best validated manually since `bot.ArchiLogger` is not easily mocked).

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `cs-inventory-config`: The `SendCSItems` default value changes from `true` to `false`. Add scenarios covering: (a) property absent → not enabled, (b) property `true` → enabled and logged, (c) property `false` → disabled and logged, (d) property invalid → disabled, warning + disabled log.
- `bot-startup-cs-scan`: The "sendcsitems enabled or unset" scenario inverts — "unset" now means the startup scan is skipped. Add a logging scenario so the startup path emits the same enable/disable info line as the trade path.
- `trade-notification`: The "SendCSItems true (or not explicitly set to false)" precondition is tightened to "SendCSItems true". Add a scenario for the new logging behavior and a scenario for "absent → no forwarding, no log".
- `plugin-code-organization`: The `CSBotConfig` scenarios that assert a `true` default are updated to assert a `false` default for missing/null/invalid inputs.

## Impact

- **Code**:
  - `CSInventoryPlugin/CSBotConfig.cs` — flip the default in every "no value" branch (null dict, missing key, invalid kind) from `true` to `false`.
  - `CSInventoryPlugin/CSInventoryPlugin.cs` — `IsSendCsItemsEnabled` returns the new value and the caller logs the enable/disable info line in `OnBotLoggedOn` and `OnBotTradeOfferResults` when the property is explicitly set. The existing "skipped because SendCSItems = false" lines are removed (the new info line covers that case and avoids double-logging).
  - The existing "Invalid SendCSItems value" warning is preserved.
- **Docs**: `README.md` — the `SendCSItems` description, the config example, and the "How it works" section are updated to reflect the new `false` default and the logging behavior. The "Zero Configuration" feature bullet is updated/removed because the plugin now ships with forwarding OFF by default.
- **Tests**: `CSInventoryPlugin.Tests/CSInventoryPluginTests.cs` — the parser tests (`TryGetSendCsItems_*_ReturnsValidAndEnabledDefault`) are updated to expect the new `enabled == false` default. The `TryGetSendCsItems_NullProperties` and `TryGetSendCsItems_Missing` tests assert `valid == true, enabled == false`. The `InvalidType` and `NumberType` tests also assert `enabled == false`.
- **User config**: Users who want forwarding ON must now set `"SendCSItems": true` explicitly. The README config example is updated to make this visible. There is no automatic migration of user JSON files; the behavior change is documented in the release notes (out of scope for this change but called out for the maintainer).
- **Runtime behavior**: For any bot whose JSON does not set `SendCSItems`, the plugin will no longer scan or forward CS items, and will not log a "skipped" line. The plugin's "initialized" / "loaded" / "scanning" log lines still fire so operators can see the plugin is alive.
