## ADDED Requirements

### Requirement: Per-bot sendcsitems configuration
Each bot SHALL support a `sendcsitems` boolean property in its ASF JSON configuration to control whether CS item trade notifications are sent from that bot.

#### Scenario: Property set to true (or not set)
- **WHEN** a bot's config has `"sendcsitems": true` or the property is absent
- **THEN** the system SHALL send CS item trade notifications for that bot

#### Scenario: Property set to false
- **WHEN** a bot's config has `"sendcsitems": false`
- **THEN** the system SHALL NOT send CS item trade notifications for that bot

#### Scenario: Property is non-boolean
- **WHEN** a bot's config has `"sendcsitems"` with a non-boolean value (e.g., string, number)
- **THEN** the system SHALL treat it as the default value (true)
- **AND** log a warning about the invalid configuration value

### Requirement: Config property accessed via plugin API

The system SHALL read the `sendcsitems` property via the `IBotModules.OnBotInitModules` API, which provides the `additionalConfigProperties` dictionary from ASF's `[JsonExtensionData]` mechanism.

#### Scenario: Plugin receives config at bot init
- **WHEN** a bot is initialized by ASF
- **THEN** the system receives the bot's `additionalConfigProperties` via `OnBotInitModules`
- **AND** stores them keyed by bot name for later retrieval

#### Scenario: Plugin reads config at trade check time
- **WHEN** CS items are detected in a bot's inventory
- **THEN** the system retrieves the bot's stored `additionalConfigProperties` by bot name
- **AND** checks for the `sendcsitems` key
- **AND** parses its value as a boolean
