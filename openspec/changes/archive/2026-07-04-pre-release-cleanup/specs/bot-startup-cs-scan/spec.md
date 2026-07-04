## MODIFIED Requirements

### Requirement: Respect sendcsitems config for startup scan
The startup scan SHALL honor the per-bot `SendCSItems` configuration flag, identical to the trade-results path.

#### Scenario: sendcsitems disabled for bot
- **WHEN** a bot starts with `"SendCSItems": false` in its configuration
- **THEN** the system SHALL NOT perform the startup CS item scan for that bot
- **AND** logs that the startup scan was skipped due to config

#### Scenario: sendcsitems enabled or unset
- **WHEN** a bot starts with `"SendCSItems": true` or the property absent
- **THEN** the system SHALL perform the startup CS item scan for that bot
