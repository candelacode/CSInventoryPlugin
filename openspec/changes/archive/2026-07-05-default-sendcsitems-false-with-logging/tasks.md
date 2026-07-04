## 1. Update CSBotConfig parser

- [x] 1.1 Change `CSBotConfig.TryGetSendCsItems()` signature to `internal static bool TryGetSendCsItems(IReadOnlyDictionary<string, JsonElement>? additionalProperties, out bool enabled, out bool explicitlySet)`.
- [x] 1.2 Flip the default in the `additionalProperties == null` branch from `enabled = true` to `enabled = false`; set `explicitlySet = false`.
- [x] 1.3 Flip the default in the "key not found" branch from `enabled = true` to `enabled = false`; set `explicitlySet = false`.
- [x] 1.4 Flip the default in the "non-boolean value" branch (the `return false` at the bottom) from `enabled = true` to `enabled = false`; set `explicitlySet = true` (the key WAS written in the JSON, even though its value is bad).
- [x] 1.5 For the `True` and `False` `JsonValueKind` branches, keep the parsed `enabled` value and set `explicitlySet = true`.
- [x] 1.6 Confirm the parser still has no side effects (no logging, no `Bot` dependency).

## 2. Update CSInventoryPlugin caller

- [x] 2.1 Update `IsSendCsItemsEnabled(bot)` to call the new 3-out `TryGetSendCsItems` overload and capture `enabled` and `explicitlySet`.
- [x] 2.2 Return the new `enabled` value to callers (the `false` default is now baked into `enabled`).
- [x] 2.3 Keep the existing "Invalid SendCSItems value, expected boolean. Using default (false)." warning when `valid == false`.
- [x] 2.4 In `OnBotLoggedOn`: when `explicitlySet == true`, emit `bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: SendCSItems is enabled.")` for `enabled == true` and `"...is disabled."` for `enabled == false`. When `explicitlySet == false`, emit no config-related log line. Remove the old "Startup CS item scan skipped (SendCSItems = false)." line.
- [x] 2.5 In `OnBotTradeOfferResults`: same logging pattern as 2.4. Remove the old "CS item notification skipped (SendCSItems = false)." line. The early-return path when `enabled == false` stays, but it now also requires `explicitlySet` consideration (a bot with no config does not need the disabled line — it just silently returns).

## 3. Update unit tests

- [x] 3.1 Update `TryGetSendCsItems_True_ReturnsValidAndEnabled` to use the new 3-out signature and assert `valid == true, enabled == true, explicitlySet == true`.
- [x] 3.2 Update `TryGetSendCsItems_False_ReturnsValidAndDisabled` to use the new 3-out signature and assert `valid == true, enabled == false, explicitlySet == true`.
- [x] 3.3 Update `TryGetSendCsItems_Missing_ReturnsValidAndEnabledDefault` (renamed in spirit to `TryGetSendCsItems_Missing_ReturnsValidAndDisabledDefault`) to assert `valid == true, enabled == false, explicitlySet == false`.
- [x] 3.4 Update `TryGetSendCsItems_NullProperties_ReturnsValidAndEnabledDefault` (renamed in spirit to `TryGetSendCsItems_NullProperties_ReturnsValidAndDisabledDefault`) to assert `valid == true, enabled == false, explicitlySet == false`.
- [x] 3.5 Update `TryGetSendCsItems_InvalidType_ReturnsInvalidAndEnabledDefault` to assert `valid == false, enabled == false, explicitlySet == true`.
- [x] 3.6 Update `TryGetSendCsItems_NumberType_ReturnsInvalidAndEnabledDefault` to assert `valid == false, enabled == false, explicitlySet == true`.

## 4. Update documentation

- [x] 4.1 In `README.md`, change the `SendCSItems` property description from `default: true` to `default: false`.
- [x] 4.2 In `README.md`, update the Features bullet "Zero Configuration" to "Opt-in forwarding" and explain the new default.
- [x] 4.3 In `README.md`, update the "How it works" step 4 from "if the bot's `SendCSItems` config is not `false`" to "if the bot's `SendCSItems` config is `true`".
- [x] 4.4 In `README.md`, add a short note under "Configuration" explaining the new logging behavior: a single info line per relevant event stating the effective state when the property is explicitly set, and silence when the property is absent.

## 5. Verify

- [x] 5.1 Run `dotnet build -c Release` (or `build.bat`) and confirm the plugin builds with no errors.
- [x] 5.2 Run `dotnet test` and confirm all tests pass, including the new default-`false` assertions.
- [x] 5.3 Run `openspec validate "default-sendcsitems-false-with-logging" --type change` and confirm the change validates cleanly.
- [ ] 5.4 (Manual) On a real ASF run, verify: a bot with no `SendCSItems` key produces no config-related log line; a bot with `"SendCSItems": true` logs "SendCSItems is enabled."; a bot with `"SendCSItems": false` logs "SendCSItems is disabled."; a bot with `"SendCSItems": "yes"` logs the warning plus "SendCSItems is disabled.".
