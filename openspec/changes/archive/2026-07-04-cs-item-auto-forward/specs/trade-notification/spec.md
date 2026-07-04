## MODIFIED Requirements

### Requirement: Log trade notification events

The system SHALL log all trade notification attempts and outcomes.

#### Scenario: Successful trade sent
- **WHEN** a trade offer is sent successfully
- **THEN** the system logs the item count, bot name, and target master account

#### Scenario: Trade skipped due to config
- **WHEN** CS items are detected but `sendcsitems` is false for the bot
- **THEN** the system logs that CS item notification was skipped for that bot
