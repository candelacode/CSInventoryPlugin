## ADDED Requirements

### Requirement: Monitor bot inventories for CS items
The system SHALL monitor bot inventories for Counter Strike (appId 730) items after trade events and item changes.

#### Scenario: CS item detected after trade
- **WHEN** a trade offer involving the bot is accepted
- **THEN** the system inspects the bot's inventory cache
- **AND** checks for items belonging to appId 730 (Counter Strike)
- **AND** if CS items are found, trigger the trade notification capability

#### Scenario: No CS items in inventory
- **WHEN** a trade offer involving the bot is accepted
- **AND** the bot's inventory contains no items from appId 730
- **THEN** the system takes no further action

#### Scenario: Inventory cache refresh before check
- **WHEN** the system is about to inspect the bot's inventory
- **THEN** it SHALL request an inventory cache refresh via `SteamInventory.RequestCacheAsync()`
- **AND** wait for the cache to be updated before checking for CS items

#### Scenario: Plugin initialization
- **WHEN** the plugin loads and a bot is connected
- **THEN** the system SHALL NOT scan existing inventory items
- **AND** only monitor new items arriving from trade offers
