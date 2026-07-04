## Purpose

Defines the internal code structure and module boundaries of the CSInventory plugin, keeping the entry point thin and CS item logic in dedicated static classes.
## Requirements
### Requirement: Plugin entry point contains only ASF lifecycle callbacks
The plugin's main class (`CSInventoryPlugin`) SHALL contain only ASF interface implementations and per-bot state management. All CS item logic (filtering, config parsing, forwarding, startup scanning) SHALL be delegated to dedicated internal modules.

#### Scenario: Entry point delegates filtering to utilities
- **WHEN** `OnBotTradeOfferResults` receives trade results
- **THEN** the entry point calls `CSItemUtilities.FilterCsItems()` to filter CS items
- **AND** does not contain inline filtering logic

#### Scenario: Entry point delegates forwarding to forwarder
- **WHEN** CS items are detected by any mechanism
- **THEN** the entry point calls `CSItemForwarder.ForwardCsItemsToMaster()`
- **AND** does not contain inline master validation or `SendInventory` calls

### Requirement: CS item logic is in a dedicated static utility class
The system SHALL provide an `internal static` `CSItemUtilities` class containing pure CS item logic: the `CSAppID` and `CSContextID` constants, the `FilterCsItems()` function, and the `EvaluateMasterForForwarding()` function with its `ForwardMasterDecision` enum. This class SHALL have no dependency on `Bot` and no side effects.

#### Scenario: Filtering CS items without a Bot instance
- **WHEN** `CSItemUtilities.FilterCsItems()` is called with a collection of `Asset` objects
- **THEN** it returns only items with `AppID == CSAppID`
- **AND** does not require a `Bot` instance

#### Scenario: Evaluating master for forwarding without a Bot instance
- **WHEN** `CSItemUtilities.EvaluateMasterForForwarding()` is called with a master SteamID and bot SteamID
- **THEN** it returns the appropriate `ForwardMasterDecision` enum value
- **AND** does not require a `Bot` instance

### Requirement: Config parsing is decoupled from Bot
The system SHALL provide an `internal static` `CSBotConfig` class that parses the `SendCSItems` property from a raw `IReadOnlyDictionary<string, JsonElement>?` without requiring a `Bot` instance. The parser SHALL look up the canonical `SendCSItems` key first and fall back to the legacy `sendcsitems` key when the canonical key is absent. The parsing result SHALL indicate whether the value was valid so the caller can log warnings for invalid configurations, and SHALL indicate whether the legacy key was the one used so the caller can log a deprecation warning. The parser SHALL have no side effects and SHALL NOT log.

#### Scenario: Parsing sendcsitems from a raw dictionary
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary containing `"SendCSItems": false`
- **THEN** it returns `false` as the enabled value
- **AND** returns `true` as the validity flag
- **AND** indicates the canonical key was used

#### Scenario: Parsing via legacy key
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary containing `"sendcsitems": false` and no `"SendCSItems"` key
- **THEN** it returns `false` as the enabled value
- **AND** returns `true` as the validity flag
- **AND** indicates the legacy key was used

#### Scenario: Parsing sendcsitems when absent
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary that does not contain `"SendCSItems"` or `"sendcsitems"`
- **THEN** it returns `true` as the enabled value (default)
- **AND** returns `true` as the validity flag

#### Scenario: Parsing sendcsitems with invalid type
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary containing `"SendCSItems": "yes"` (non-boolean)
- **THEN** it returns `true` as the enabled value (default)
- **AND** returns `false` as the validity flag

#### Scenario: Canonical key takes precedence over legacy key
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary containing both `"SendCSItems": true` and `"sendcsitems": false`
- **THEN** it returns `true` as the enabled value (from the canonical key)
- **AND** returns `true` as the validity flag
- **AND** indicates the canonical key was used

### Requirement: Forwarding and startup scan logic is in a dedicated static class
The system SHALL provide an `internal static` `CSItemForwarder` class that encapsulates the forwarding logic (`ForwardCsItemsToMaster`) and startup scan logic (`PerformStartupScan`). This class depends on `Bot`, `CSItemUtilities`, and `CSBotConfig`.

#### Scenario: Forwarding CS items to master
- **WHEN** `CSItemForwarder.ForwardCsItemsToMaster()` is called with a bot and CS items
- **THEN** it validates the master account using `CSItemUtilities`
- **AND** sends the trade offer via `Bot.Actions.SendInventory()`
- **AND** logs the outcome

#### Scenario: Performing startup scan
- **WHEN** `CSItemForwarder.PerformStartupScan()` is called with a bot
- **THEN** it fetches the bot's CS inventory using `CSItemUtilities.CSAppID` and `CSItemUtilities.CSContextID`
- **AND** forwards any found items via `ForwardCsItemsToMaster()`

