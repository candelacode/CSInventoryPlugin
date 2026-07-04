## ADDED Requirements

### Requirement: Scan bot inventory for CS items on startup
When a bot finishes initialization and is connected and logged on, the system SHALL scan that bot's inventory for Counter Strike (appId 730) items and forward them to the configured master account.

#### Scenario: Bot starts with CS items in inventory
- **WHEN** a bot becomes connected and logged on after startup
- **AND** the bot's inventory contains items with appId 730
- **THEN** the system refreshes the bot's inventory
- **AND** collects all CS (appId 730) items
- **AND** forwards them to the bot's configured master account via a trade offer

#### Scenario: Bot starts with no CS items in inventory
- **WHEN** a bot becomes connected and logged on after startup
- **AND** the bot's inventory contains no items with appId 730
- **THEN** the system takes no further action

#### Scenario: Bot not connected at startup
- **WHEN** a bot is initialized but not yet connected and logged on
- **THEN** the system SHALL NOT perform the startup inventory scan

### Requirement: Respect sendcsitems config for startup scan
The startup scan SHALL honor the per-bot `sendcsitems` configuration flag, identical to the trade-results path.

#### Scenario: sendcsitems disabled for bot
- **WHEN** a bot starts with `"sendcsitems": false` in its configuration
- **THEN** the system SHALL NOT perform the startup CS item scan for that bot
- **AND** logs that the startup scan was skipped due to config

#### Scenario: sendcsitems enabled or unset
- **WHEN** a bot starts with `"sendcsitems": true` or the property absent
- **THEN** the system SHALL perform the startup CS item scan for that bot

### Requirement: Skip startup scan when master is invalid
The startup scan SHALL validate the master account before sending, matching the trade-notification rules.

#### Scenario: No master configured
- **WHEN** the startup scan detects CS items
- **AND** the bot has no master account configured
- **THEN** the system SHALL log a warning and not send a trade offer

#### Scenario: Master account is the bot itself
- **WHEN** the startup scan detects CS items
- **AND** the bot's master account ID equals the bot's own Steam ID
- **THEN** the system SHALL NOT send a trade offer
- **AND** logs a warning that master is self

### Requirement: Log startup scan events
The system SHALL log all startup scan attempts and outcomes.

#### Scenario: Successful startup forward
- **WHEN** the startup scan forwards CS items to the master account successfully
- **THEN** the system logs the item count, bot name, and target master account

#### Scenario: Startup forward fails
- **WHEN** the startup scan attempts to send CS items and `Bot.Actions.SendInventory()` fails
- **THEN** the system logs the failure reason
- **AND** does not retry automatically
