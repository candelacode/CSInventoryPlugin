## MODIFIED Requirements

### Requirement: Send trade notification to master account
When CS items are detected in a bot's inventory by any supported mechanism (trade results processing or bot startup scan), the system SHALL send a trade offer containing those CS items to the configured master account — but only when the bot's `SendCSItems` config is explicitly set to `true`. When the property is absent, forwarding is off and no config-related log line is emitted.

#### Scenario: Send trade offer with CS items
- **WHEN** CS items are detected in a bot's inventory by any supported mechanism
- **AND** the bot's `SendCSItems` configuration is `true`
- **THEN** the system creates a trade offer from the bot to the master account
- **AND** includes all detected CS items in the trade offer
- **AND** sends the trade offer via `Bot.Actions.SendInventory()`

#### Scenario: sendcsitems absent — no forwarding, no log
- **WHEN** CS items are detected in a bot's inventory by any supported mechanism
- **AND** the bot's config does not contain `"SendCSItems"`
- **THEN** the system SHALL NOT send a trade offer
- **AND** SHALL NOT emit a config-related log line for that bot

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

## REMOVED Requirements

### Requirement: Trade skipped due to config
**Reason**: The old "skipped" line is replaced by the new "explicit state" info line in `cs-inventory-config` (Requirement: Log explicit sendcsitems state). The new line covers both the explicit-`false` and the explicit-`true` cases; emitting a separate "skipped" line would double-log.
**Migration**: Operators who relied on grepping for "skipped" should now grep for `SendCSItems is disabled.` (explicit `false`) or rely on the absence of any config-related log line (property absent, no trade offer made).

## ADDED Requirements

### Requirement: Log explicit sendcsitems state on trade results
The system SHALL emit a single info log line stating the effective `SendCSItems` state per bot, per trade-results event, whenever the property is explicitly set. When the property is absent, the system SHALL stay silent.

#### Scenario: Explicitly enabled on trade results
- **WHEN** trade results arrive for a bot with `"SendCSItems": true`
- **THEN** the system logs an info line stating that `SendCSItems` is enabled for that bot

#### Scenario: Explicitly disabled on trade results
- **WHEN** trade results arrive for a bot with `"SendCSItems": false`
- **THEN** the system logs an info line stating that `SendCSItems` is disabled for that bot
- **AND** does not attempt to forward CS items
