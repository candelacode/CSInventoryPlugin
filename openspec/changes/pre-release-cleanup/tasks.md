## 1. Remove legacy `sendcsitems` code

- [ ] 1.1 In `CSInventoryPlugin/CSBotConfig.cs`, change `TryGetSendCsItems` to the 2-parameter signature `internal static bool TryGetSendCsItems(IReadOnlyDictionary<string, JsonElement>? additionalProperties, out bool enabled)` and drop the `usedLegacyKey` out parameter.
- [ ] 1.2 In `CSBotConfig.cs`, remove the legacy `TryGetValue("sendcsitems", ...)` fallback branch so the parser only looks up `"SendCSItems"`.
- [ ] 1.3 In `CSInventoryPlugin/CSInventoryPlugin.cs` `IsSendCsItemsEnabled`, drop the `out bool usedLegacyKey` capture and remove the `if (usedLegacyKey) { ... LogGenericWarning ... }` deprecation block.
- [ ] 1.4 In `CSInventoryPlugin.Tests/CSInventoryPluginTests.cs`, drop the 4 legacy-fallback / both-keys test cases: `TryGetSendCsItems_LegacyKey_True_...`, `TryGetSendCsItems_LegacyKey_False_...`, `TryGetSendCsItems_LegacyKey_InvalidType_...`, `TryGetSendCsItems_BothKeys_CanonicalWins`.
- [ ] 1.5 In the remaining `TryGetSendCsItems_*` test cases, drop the `out bool usedLegacyKey` parameter from the call and remove the `Assert.False(usedLegacyKey)` / `Assert.True(usedLegacyKey)` assertions, restoring the simpler 2-parameter call shape.

## 2. Fix namespace/folder mismatch (IDE0130)

- [ ] 2.1 In `CSInventoryPlugin/CSBotConfig.cs`, change `namespace CSInventory.Plugin;` to `namespace CSInventoryPlugin;`.
- [ ] 2.2 In `CSInventoryPlugin/CSInventoryPlugin.cs`, change `namespace CSInventory.Plugin;` to `namespace CSInventoryPlugin;`.
- [ ] 2.3 In `CSInventoryPlugin/CSItemForwarder.cs`, change `namespace CSInventory.Plugin;` to `namespace CSInventoryPlugin;`.
- [ ] 2.4 In `CSInventoryPlugin/CSItemUtilities.cs`, change `namespace CSInventory.Plugin;` to `namespace CSInventoryPlugin;`.
- [ ] 2.5 In `CSInventoryPlugin.Tests/CSInventoryPluginTests.cs`, change `using CSInventory.Plugin;` to `using CSInventoryPlugin;` and change `namespace CSInventory.Plugin.Tests;` to `namespace CSInventoryPlugin.Tests;`.

## 3. Suppress NU1903 in build config

- [ ] 3.1 In `Directory.Build.props`, add a `<NoWarn>$(NoWarn);NU1903</NoWarn>` entry with an XML comment explaining that the warning is a transitive High-severity vulnerability in `Microsoft.OpenApi` 2.0.0, pulled in by `Microsoft.AspNetCore.OpenAPI` 10.0.8 (the version ASF upstream pins in `ArchiSteamFarm/Directory.Packages.props`), and that the proper fix is upstream.

## 4. Verify

- [ ] 4.1 Run `dotnet build -c Release` and confirm 0 errors and no `NU1903` warnings.
- [ ] 4.2 Run `dotnet test` and confirm all remaining tests pass (the legacy-fallback test cases are removed; the simplified test set covers true/false/missing/null/invalid-type/number).
- [ ] 4.3 Run `openspec validate "pre-release-cleanup" --type change` and confirm the change validates cleanly.
