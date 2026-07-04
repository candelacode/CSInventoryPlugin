## MODIFIED Requirements

### Requirement: Per-bot sendcsitems configuration
Each bot SHALL support a `SendCSItems` boolean property in its ASF JSON configuration to control whether CS item trade notifications are sent from that bot. The property name is `SendCSItems` (PascalCase, matching ASF config conventions). When the property is absent, the system SHALL treat the value as `false` (forwarding disabled by default; opt-in).

#### Scenario: Property set to true
- **WHEN** a bot's config has `"SendCSItems": true`
- **THEN** the system SHALL send CS item trade notifications for that bot

#### Scenario: Property set to false
- **WHEN** a bot's config has `"SendCSItems": false`
- **THEN** the system SHALL NOT send CS item trade notifications for that bot

#### Scenario: Property absent
- **WHEN** a bot's config does not contain `"SendCSItems"` (or `additionalConfigProperties` is null)
- **THEN** the system SHALL NOT send CS item trade notifications for that bot
- **AND** SHALL NOT emit a config-related log line for that bot

#### Scenario: Property is non-boolean
- **WHEN** a bot's config has `"SendCSItems"` with a non-boolean value (e.g., string, number)
- **THEN** the system SHALL treat it as the default value (false)
- **AND** log a warning about the invalid configuration value

## ADDED Requirements

### Requirement: Log explicit sendcsitems state
The system SHALL emit a single info log line per bot, per relevant event, that states the effective state of `SendCSItems` whenever the property is explicitly set in the bot's JSON config. The system SHALL NOT emit a config-related log line when the property is absent.

#### Scenario: Explicitly enabled
- **WHEN** a bot's config has `"SendCSItems": true`
- **THEN** the system SHALL log an info line stating that `SendCSItems` is enabled for that bot

#### Scenario: Explicitly disabled
- **WHEN** a bot's config has `"SendCSItems": false`
- **THEN** the system SHALL log an info line stating that `SendCSItems` is disabled for that bot

#### Scenario: Property absent — no log
- **WHEN** a bot's config does not contain `"SendCSItems"`
- **THEN** the system SHALL NOT emit any config-related log line for that bot

#### Scenario: Invalid value — warning plus disabled info line
- **WHEN** a bot's config has `"SendCSItems"` with a non-boolean value
- **THEN** the system SHALL emit the invalid-value warning
- **AND** SHALL emit the "disabled" info line (because the effective value falls back to the new `false` default)
