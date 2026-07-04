## MODIFIED Requirements

### Requirement: Send trade notification to master account
When CS items are detected in a bot's inventory by any supported mechanism (trade results processing or bot startup scan), the system SHALL send a trade offer containing those CS items to the configured master account.

#### Scenario: Send trade offer with CS items
- **WHEN** CS items are detected in a bot's inventory by any supported mechanism
- **AND** the bot's `SendCSItems` configuration is true (or not explicitly set to false)
- **THEN** the system creates a trade offer from the bot to the master account
- **AND** includes all detected CS items in the trade offer
- **AND** sends the trade offer via `Bot.Actions.SendInventory()`

#### Scenario: Master account is the bot itself
- **WHEN** the bot's master account ID matches the bot's own Steam ID
- **THEN** the system SHALL NOT send a trade offer to avoid self-trading
- **AND** log a warning that CS items were found but master is self

#### Scenario: No master configured
- **WHEN** a bot has no master account configured
- **THEN** the system SHALL log a warning
- **AND** not attempt to send a trade notification

#### Scenario: Trade offer sending fails
- **WHEN** `Bot.Actions.SendInventory()` fails for any reason
- **THEN** the system SHALL log the error
- **AND** not retry automatically

### Requirement: Log trade notification events

The system SHALL log all trade notification attempts and outcomes.

#### Scenario: Successful trade sent
- **WHEN** a trade offer is sent successfully
- **THEN** the system logs the item count, bot name, and target master account

#### Scenario: Trade skipped due to config
- **WHEN** CS items are detected but `SendCSItems` is false for the bot
- **THEN** the system logs that CS item notification was skipped for that bot
