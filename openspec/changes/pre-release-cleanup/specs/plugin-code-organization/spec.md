## MODIFIED Requirements

### Requirement: Config parsing is decoupled from Bot
The system SHALL provide an `internal static` `CSBotConfig` class that parses the `SendCSItems` property from a raw `IReadOnlyDictionary<string, JsonElement>?` without requiring a `Bot` instance. The parsing result SHALL indicate whether the value was valid so the caller can log warnings for invalid configurations. The parser SHALL have no side effects and SHALL NOT log.

#### Scenario: Parsing sendcsitems from a raw dictionary
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary containing `"SendCSItems": false`
- **THEN** it returns `false` as the enabled value
- **AND** returns `true` as the validity flag

#### Scenario: Parsing sendcsitems when absent
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary that does not contain `"SendCSItems"`
- **THEN** it returns `true` as the enabled value (default)
- **AND** returns `true` as the validity flag

#### Scenario: Parsing sendcsitems with invalid type
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary containing `"SendCSItems": "yes"` (non-boolean)
- **THEN** it returns `true` as the enabled value (default)
- **AND** returns `false` as the validity flag
