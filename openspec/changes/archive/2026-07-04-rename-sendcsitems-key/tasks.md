## 1. Update CSBotConfig parser

- [x] 1.1 Change `CSBotConfig.TryGetSendCsItems()` signature to `internal static bool TryGetSendCsItems(IReadOnlyDictionary<string, JsonElement>? additionalProperties, out bool enabled, out bool usedLegacyKey)`.
- [x] 1.2 Implement canonical-first lookup: `TryGetValue("SendCSItems", ...)` first; if found, parse it and set `usedLegacyKey = false`.
- [x] 1.3 Implement legacy fallback: when `SendCSItems` is absent, `TryGetValue("sendcsitems", ...)`; if found, parse it and set `usedLegacyKey = true`.
- [x] 1.4 Preserve existing validity semantics: `True`/`False` kinds return `valid = true`; missing or non-boolean returns default `enabled = true` with `valid` as before (`true` for missing, `false` for non-boolean). Set `usedLegacyKey = false` when neither key is present.
- [x] 1.5 Ensure the parser has no side effects (no logging, no `Bot` dependency).

## 2. Update CSInventoryPlugin caller

- [x] 2.1 Update `IsSendCsItemsEnabled(bot)` to call the new 3-out/`usedLegacyKey` overload and capture `usedLegacyKey`.
- [x] 2.2 When `usedLegacyKey` is `true`, emit `bot.ArchiLogger.LogGenericWarning` with a deprecation message naming the legacy `sendcsitems` key and advising migration to `SendCSItems`.
- [x] 2.3 Update the invalid-value warning string in `IsSendCsItemsEnabled` to reference `SendCSItems` (instead of `sendcsitems`).
- [x] 2.4 Update the skip log strings in `OnBotLoggedOn` and `OnBotTradeOfferResults` to reference `SendCSItems = false` (instead of `sendcsitems = false`).

## 3. Update unit tests

- [x] 3.1 Update `TryGetSendCsItems_True_ReturnsValidAndEnabled` to use `"SendCSItems": true` and assert `usedLegacyKey == false`.
- [x] 3.2 Update `TryGetSendCsItems_False_ReturnsValidAndDisabled` to use `"SendCSItems": false` and assert `usedLegacyKey == false`.
- [x] 3.3 Update `TryGetSendCsItems_InvalidType_ReturnsInvalidAndEnabledDefault` and `TryGetSendCsItems_NumberType_ReturnsInvalidAndEnabledDefault` to use `"SendCSItems"` key.
- [x] 3.4 Keep `TryGetSendCsItems_Missing_ReturnsValidAndEnabledDefault` and `TryGetSendCsItems_NullProperties_ReturnsValidAndEnabledDefault` asserting the default and `usedLegacyKey == false`.
- [x] 3.5 Add `TryGetSendCsItems_LegacyKey_True_ReturnsValidEnabledAndLegacyFlag` using `"sendcsitems": true` and asserting `usedLegacyKey == true`.
- [x] 3.6 Add `TryGetSendCsItems_LegacyKey_False_ReturnsValidDisabledAndLegacyFlag` using `"sendcsitems": false` and asserting `usedLegacyKey == true` and `enabled == false`.
- [x] 3.7 Add `TryGetSendCsItems_LegacyKey_InvalidType_ReturnsInvalidDefaultAndLegacyFlag` using `"sendcsitems": "yes"` and asserting `valid == false`, `enabled == true`, `usedLegacyKey == true`.
- [x] 3.8 Add `TryGetSendCsItems_BothKeys_CanonicalWins` using both `"SendCSItems": true` and `"sendcsitems": false`, asserting `enabled == true` and `usedLegacyKey == false`.

## 4. Update documentation

- [x] 4.1 In `README.md`, change the config example JSON to `"SendCSItems": true`.
- [x] 4.2 In `README.md`, update the property description to name `SendCSItems` as the canonical key (bool, optional, default `true`) and note `sendcsitems` as a deprecated alias that still works.
- [x] 4.3 In `README.md`, update the Features bullet and the "How it works" step that reference `sendcsitems` to reference `SendCSItems`.

## 5. Verify

- [x] 5.1 Run `dotnet build -c Release` (or `build.bat`) and confirm the plugin builds with no errors.
- [x] 5.2 Run `dotnet test` and confirm all tests pass, including the new legacy-fallback and both-keys cases.
- [x] 5.3 Run `openspec validate "rename-sendcsitems-key" --type change` and confirm the change validates cleanly.
