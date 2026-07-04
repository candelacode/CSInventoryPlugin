## MODIFIED Requirements

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
