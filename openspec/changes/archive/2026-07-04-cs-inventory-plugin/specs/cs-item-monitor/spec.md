## ADDED Requirements

### Requirement: Monitor trade results for CS items
The system SHALL inspect processed trade results for Counter Strike (appId 730) items received by the bot and trigger trade notifications when found.

#### Scenario: CS item detected in trade results
- **WHEN** ASF notifies the plugin via `OnBotTradeOfferResults()`
- **THEN** the system inspects `ParseTradeResult.ItemsToReceive` from each trade result
- **AND** checks for items belonging to appId 730 (Counter Strike)
- **AND** if CS items are found, trigger the trade notification capability

#### Scenario: No CS items in trade results
- **WHEN** ASF notifies the plugin via `OnBotTradeOfferResults()`
- **AND** none of the `ParseTradeResult.ItemsToReceive` collections contain items from appId 730
- **THEN** the system takes no further action

#### Scenario: Bot not connected
- **WHEN** ASF notifies the plugin via `OnBotTradeOfferResults()`
- **AND** the bot is not connected and logged on
- **THEN** the system takes no further action

#### Scenario: Empty trade results
- **WHEN** ASF notifies the plugin via `OnBotTradeOfferResults()`
- **AND** the trade results collection is null or empty
- **THEN** the system takes no further action
