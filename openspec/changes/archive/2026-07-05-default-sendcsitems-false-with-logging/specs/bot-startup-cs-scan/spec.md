## MODIFIED Requirements

### Requirement: Respect sendcsitems config for startup scan
The startup scan SHALL honor the per-bot `SendCSItems` configuration flag, identical to the trade-results path. Forwarding is opt-in: the scan is performed only when the property is explicitly set to `true`.

#### Scenario: sendcsitems disabled for bot
- **WHEN** a bot starts with `"SendCSItems": false` in its configuration
- **THEN** the system SHALL NOT perform the startup CS item scan for that bot
- **AND** logs that `SendCSItems` is disabled for that bot

#### Scenario: sendcsitems explicitly enabled
- **WHEN** a bot starts with `"SendCSItems": true` in its configuration
- **THEN** the system SHALL perform the startup CS item scan for that bot
- **AND** logs that `SendCSItems` is enabled for that bot

#### Scenario: sendcsitems absent
- **WHEN** a bot starts without `"SendCSItems"` in its configuration (or `additionalConfigProperties` is null)
- **THEN** the system SHALL NOT perform the startup CS item scan for that bot
- **AND** SHALL NOT emit a config-related log line for that bot
