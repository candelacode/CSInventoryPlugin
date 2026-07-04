## MODIFIED Requirements

### Requirement: Per-bot sendcsitems configuration
Each bot SHALL support a `SendCSItems` boolean property in its ASF JSON configuration to control whether CS item trade notifications are sent from that bot. The canonical property name is `SendCSItems` (PascalCase, matching ASF config conventions). The system SHALL also accept the legacy `sendcsitems` (all-lowercase) key for backward compatibility.

#### Scenario: Property set to true (or not set)
- **WHEN** a bot's config has `"SendCSItems": true` or the property is absent
- **THEN** the system SHALL send CS item trade notifications for that bot

#### Scenario: Property set to false
- **WHEN** a bot's config has `"SendCSItems": false`
- **THEN** the system SHALL NOT send CS item trade notifications for that bot

#### Scenario: Property is non-boolean
- **WHEN** a bot's config has `"SendCSItems"` with a non-boolean value (e.g., string, number)
- **THEN** the system SHALL treat it as the default value (true)
- **AND** log a warning about the invalid configuration value

#### Scenario: Legacy lowercase key used
- **WHEN** a bot's config has the legacy `"sendcsitems"` key and does not have the canonical `"SendCSItems"` key
- **THEN** the system SHALL honor the value of the legacy key as if it were `SendCSItems`
- **AND** log a deprecation warning advising migration to `SendCSItems`

#### Scenario: Both canonical and legacy keys present
- **WHEN** a bot's config has both `"SendCSItems"` and `"sendcsitems"`
- **THEN** the system SHALL use the value of the canonical `"SendCSItems"` key
- **AND** log a deprecation warning stating that the legacy `"sendcsitems"` key is present and ignored

### Requirement: Config property accessed via plugin API

The system SHALL read the `SendCSItems` property via the `IBotModules.OnBotInitModules` API, which provides the `additionalConfigProperties` dictionary from ASF's `[JsonExtensionData]` mechanism. When the canonical `SendCSItems` key is absent, the system SHALL fall back to the legacy `sendcsitems` key.

#### Scenario: Plugin receives config at bot init
- **WHEN** a bot is initialized by ASF
- **THEN** the system receives the bot's `additionalConfigProperties` via `OnBotInitModules`
- **AND** stores them keyed by bot name for later retrieval

#### Scenario: Plugin reads config at trade check time
- **WHEN** CS items are detected in a bot's inventory
- **THEN** the system retrieves the bot's stored `additionalConfigProperties` by bot name
- **AND** checks for the canonical `SendCSItems` key first
- **AND** falls back to the legacy `sendcsitems` key when the canonical key is absent
- **AND** parses the resolved value as a boolean
