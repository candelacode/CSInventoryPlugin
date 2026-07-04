## Context

The plugin currently has all logic in a single `CSInventoryPlugin.cs` (200 lines) that implements 6 ASF interfaces and contains filtering, config parsing, master validation, and forwarding logic inline. The official ASF plugins follow a different pattern: `ItemsMatcherPlugin.cs` is a slim entry point (152 lines) that delegates to `MatchingUtilities.cs` (pure static utilities), `Commands.cs` (command handling), `RemoteCommunication.cs` (per-bot state), and `Backend.cs`. This separation makes each file focused, testable, and navigable.

## Goals / Non-Goals

**Goals:**
- Split the monolithic `CSInventoryPlugin.cs` into focused files with single responsibilities.
- Decouple config parsing from `Bot` so it can be unit-tested without ASF infrastructure.
- Follow the ASF official plugin file-organization pattern (slim entry point + static utilities + logic modules).
- Keep all existing tests passing and improve test coverage by testing decoupled modules.

**Non-Goals:**
- Changing any external behavior of the plugin.
- Adding new features or ASF interfaces.
- Introducing new NuGet dependencies.
- Refactoring the test project structure (only test content changes).

## Decisions

### Decision 1: Four-file split with clear responsibilities
Split into:
- `CSInventoryPlugin.cs` — plugin entry point. Contains only ASF interface implementations (`Name`, `Version`, `OnLoaded`, `OnASFInit`, `OnBotInit`, `OnBotDestroy`, `OnBotInitModules`, `OnBotDisconnected`, `OnBotLoggedOn`, `OnBotTradeOfferResults`) and the per-bot state dictionaries. Delegates all logic to the other modules.
- `CSItemUtilities.cs` — `internal static` class with pure functions: `CSAppID`/`CSContextID` constants, `FilterCsItems()`, `EvaluateMasterForForwarding()`, `ForwardMasterDecision` enum. No side effects, no `Bot` dependency.
- `CSBotConfig.cs` — `internal static` class for `sendcsitems` config parsing. Takes `IReadOnlyDictionary<string, JsonElement>?` and returns a `bool` (or a small result struct). No `Bot` dependency — logging of invalid values is done by the caller.
- `CSItemForwarder.cs` — `internal static` class for forwarding + startup-scan logic. Contains `ForwardCsItemsToMaster()` and `PerformStartupScan()`. Depends on `Bot`, `CSItemUtilities`, and `CSBotConfig`.

**Rationale:** Mirrors the ItemsMatcher pattern where `ItemsMatcherPlugin.cs` delegates to `MatchingUtilities.cs` and `RemoteCommunication.cs`. Each file has one reason to change.
**Alternatives considered:**
- Two-file split (entry point + utilities) — rejected: config parsing and forwarding are distinct concerns with different dependencies.
- Instance classes instead of static — rejected: the plugin has no per-instance state in utilities; static matches ItemsMatcher's `MatchingUtilities`.

### Decision 2: Decouple config parsing from `Bot`
`CSBotConfig.TryGetSendCsItems` takes `IReadOnlyDictionary<string, JsonElement>?` and returns `(bool Enabled, bool IsValid)` — the caller handles logging invalid values. This makes config parsing unit-testable without mocking `Bot`.

**Rationale:** The current `GetSendCsItemsConfig(Bot)` can't be tested without a real or mocked `Bot` instance (which is sealed). Decoupling enables pure unit tests.
**Alternatives considered:**
- Keep `Bot` dependency — rejected: untestable without integration testing.

### Decision 3: Move constants to `CSItemUtilities`
`CSAppID` and `CSContextID` move from the plugin class to `CSItemUtilities` as `internal const`. The plugin entry point references them via `CSItemUtilities.CSAppID` where needed (only in `OnBotLoggedOn` which delegates to `CSItemForwarder`).

**Rationale:** Constants belong with the logic that uses them. `CSItemForwarder` uses them for the inventory fetch, and `CSItemUtilities` is the natural home for CS-specific values.
**Alternatives considered:**
- Dedicated `CSConstants.cs` — rejected: too granular for just two constants.

### Decision 4: Keep per-bot state dictionaries in the plugin entry point
`BotAdditionalProperties` and `BotStartupScanned` remain as `static ConcurrentDictionary` fields in `CSInventoryPlugin.cs` because they are populated/consumed by ASF lifecycle callbacks (`OnBotInitModules`, `OnBotDestroy`, `OnBotLoggedOn`) which live in the entry point.

**Rationale:** The dictionaries are tied to the plugin's lifecycle, not to utility logic. Moving them to a separate class would add indirection without clarity.
**Alternatives considered:**
- Move to a `BotStateStore` class — rejected: unnecessary indirection for two dictionaries accessed only from the entry point.

## Risks / Trade-offs

- [Behavioral drift during refactor] Splitting code could accidentally change behavior. → Mitigation: existing tests must all pass after refactoring; no logic changes, only moves.
- [Visibility changes] Moving methods from `private` to `internal static` broadens accessibility within the assembly. → Mitigation: `internal` is the minimum needed for `InternalsVisibleTo` test access; acceptable for a single-assembly plugin.
- [Test churn] Tests are rewritten to use new module APIs. → Mitigation: new tests cover the same cases plus new config-parsing tests that were previously impossible.
