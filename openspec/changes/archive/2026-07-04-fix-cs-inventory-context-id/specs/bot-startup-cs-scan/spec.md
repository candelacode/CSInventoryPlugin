## MODIFIED Requirements

### Requirement: Scan bot inventory for CS items on startup
When a bot finishes initialization and is connected and logged on, the system SHALL scan that bot's CS game inventory (appId 730, contextID 2) for Counter Strike items and forward them to the configured master account.

#### Scenario: Bot starts with CS items in inventory
- **WHEN** a bot becomes connected and logged on after startup
- **AND** the bot's CS game inventory (appId 730, contextID 2) contains items
- **THEN** the system fetches the bot's inventory using appId 730 and contextID 2
- **AND** collects all items from that inventory
- **AND** forwards them to the bot's configured master account via a trade offer

#### Scenario: Bot starts with no CS items in inventory
- **WHEN** a bot becomes connected and logged on after startup
- **AND** the bot's CS game inventory (appId 730, contextID 2) contains no items
- **THEN** the system takes no further action

#### Scenario: Bot not connected at startup
- **WHEN** a bot is initialized but not yet connected and logged on
- **THEN** the system SHALL NOT perform the startup inventory scan
