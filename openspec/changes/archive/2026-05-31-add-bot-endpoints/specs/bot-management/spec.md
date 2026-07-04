## ADDED Requirements

### Requirement: Enable bot
The system SHALL enable a bot by extracting its archived configuration files.

#### Scenario: Enable existing disabled bot
- **WHEN** a POST request is made to `/api/bot/enable` with bot name
- **THEN** the system extracts the bot's zip archive to the config directory
- **AND** the zip file is deleted after successful extraction

#### Scenario: Bot already enabled
- **WHEN** a POST request is made to enable a bot that is already enabled
- **THEN** the system returns success without error

### Requirement: Disable bot
The system SHALL disable a bot by archiving its configuration files.

#### Scenario: Disable existing enabled bot
- **WHEN** a POST request is made to `/api/bot/disable` with bot name
- **THEN** the system creates a zip archive of the bot's config files
- **AND** the original config files are deleted after successful archiving

#### Scenario: Bot already disabled
- **WHEN** a POST request is made to disable a bot that is already disabled
- **THEN** the system returns success without error

### Requirement: Get all bots status
The system SHALL return the status of all configured bots.

#### Scenario: List bots with status
- **WHEN** a GET request is made to `/api/bot/status`
- **THEN** the system returns a JSON object with two arrays: "enabled" and "disabled"
- **AND** each array contains bot names as strings

#### Scenario: No bots configured
- **WHEN** a GET request is made to `/api/bot/status`
- **THEN** the system returns empty arrays for both enabled and disabled