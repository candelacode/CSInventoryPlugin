## MODIFIED Requirements

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
