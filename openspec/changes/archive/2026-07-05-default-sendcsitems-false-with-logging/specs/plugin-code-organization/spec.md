## MODIFIED Requirements

### Requirement: Config parsing is decoupled from Bot
The system SHALL provide an `internal static` `CSBotConfig` class that parses the `SendCSItems` property from a raw `IReadOnlyDictionary<string, JsonElement>?` without requiring a `Bot` instance. The parsing result SHALL indicate whether the value was valid, whether the property was explicitly set, and the effective enabled value (defaulting to `false` when the property is absent, the dictionary is null, or the value is not a boolean). The parser SHALL have no side effects and SHALL NOT log.

#### Scenario: Parsing sendcsitems from a raw dictionary
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary containing `"SendCSItems": false`
- **THEN** it returns `false` as the enabled value
- **AND** returns `true` as the validity flag
- **AND** returns `true` as the explicitly-set flag

#### Scenario: Parsing sendcsitems when absent
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary that does not contain `"SendCSItems"`
- **THEN** it returns `false` as the enabled value (default)
- **AND** returns `true` as the validity flag
- **AND** returns `false` as the explicitly-set flag

#### Scenario: Parsing sendcsitems with invalid type
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with a dictionary containing `"SendCSItems": "yes"` (non-boolean)
- **THEN** it returns `false` as the enabled value (default)
- **AND** returns `false` as the validity flag
- **AND** returns `true` as the explicitly-set flag

#### Scenario: Parsing sendcsitems with null additionalProperties
- **WHEN** `CSBotConfig.TryGetSendCsItems()` is called with `additionalProperties == null`
- **THEN** it returns `false` as the enabled value (default)
- **AND** returns `true` as the validity flag
- **AND** returns `false` as the explicitly-set flag
