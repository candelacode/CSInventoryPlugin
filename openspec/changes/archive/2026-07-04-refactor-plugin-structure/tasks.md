## 1. Create CSItemUtilities static class

- [x] 1.1 Create `CSInventoryPlugin/CSItemUtilities.cs` with `internal static class CSItemUtilities` containing the `CSAppID` (730) and `CSContextID` (2) constants, the `ForwardMasterDecision` enum, `FilterCsItems()`, and `EvaluateMasterForForwarding()` functions moved from `CSInventoryPlugin.cs`.
- [x] 1.2 Verify the file compiles (`dotnet build`) with the constants and functions in their new location.

## 2. Create CSBotConfig static class

- [x] 2.1 Create `CSInventoryPlugin/CSBotConfig.cs` with `internal static class CSBotConfig` containing a `TryGetSendCsItems(IReadOnlyDictionary<string, JsonElement>?, out bool enabled)` method that returns `bool` (validity) and sets the `enabled` out parameter. This replaces the current `GetSendCsItemsConfig(Bot)` — no `Bot` dependency, no logging inside.
- [x] 2.2 Verify the file compiles.

## 3. Create CSItemForwarder static class

- [x] 3.1 Create `CSInventoryPlugin/CSItemForwarder.cs` with `internal static class CSItemForwarder` containing `ForwardCsItemsToMaster(Bot, HashSet<Asset>)` and `PerformStartupScan(Bot)` methods moved/adapted from `CSInventoryPlugin.cs`. The forwarder uses `CSItemUtilities` for filtering/master-validation and `CSBotConfig` for config parsing.
- [x] 3.2 Verify the file compiles.

## 4. Slim down CSInventoryPlugin.cs

- [x] 4.1 Remove the moved logic from `CSInventoryPlugin.cs` (constants, `FilterCsItems`, `EvaluateMasterForForwarding`, `ForwardMasterDecision`, `ShouldForwardToMaster`, `ForwardCsItemsToMaster`, `GetSendCsItemsConfig`). Keep only ASF interface implementations and the per-bot state dictionaries.
- [x] 4.2 Update `OnBotLoggedOn` to delegate the scan to `CSItemForwarder.PerformStartupScan(bot)`.
- [x] 4.3 Update `OnBotTradeOfferResults` to use `CSItemUtilities.FilterCsItems()` and `CSItemForwarder.ForwardCsItemsToMaster()`.
- [x] 4.4 Update config checking in the entry point to use `CSBotConfig.TryGetSendCsItems()` with the stored `BotAdditionalProperties`, logging invalid values at the call site.
- [x] 4.5 Verify `dotnet build -c Release` succeeds with 0 errors.

## 5. Update tests

- [x] 5.1 Update `CSInventoryPluginTests.cs` to reference `CSItemUtilities.FilterCsItems`, `CSItemUtilities.EvaluateMasterForForwarding`, and `CSItemUtilities.ForwardMasterDecision` instead of `CSInventoryPlugin.*`.
- [x] 5.2 Add tests for `CSBotConfig.TryGetSendCsItems`: true, false, missing, invalid type — verifying both the `enabled` out parameter and the validity return value.
- [x] 5.3 Replace the `ConcurrentDictionary`-based reconnect-guard tests with tests that call the plugin's `OnBotDestroy` followed by checking that a new scan would be allowed (test through the plugin's actual API, not framework behavior).
- [x] 5.4 Update the `CSContextID` guard test to reference `CSItemUtilities.CSContextID`.
- [x] 5.5 Run `dotnet test` and ensure all tests pass.

## 6. Final verification

- [x] 6.1 Run `dotnet build -c Release` and confirm 0 errors.
- [x] 6.2 Run `openspec validate refactor-plugin-structure` and confirm the change validates cleanly.
- [x] 6.3 Verify the built `CSInventoryPlugin.dll` is produced and copied to the ASF plugins directory.
