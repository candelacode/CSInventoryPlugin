## ADDED Requirements

### Requirement: ASF configuration
The system SHALL read ASF configuration from ASF's options system.

#### Scenario: Configuration available
- **WHEN** the plugin is loaded
- **THEN** the system reads AsfFolderPath from ASF configuration
- **AND** the system reads AsfApiBaseUrl from ASF configuration

#### Scenario: Missing configuration
- **WHEN** required configuration values are missing
- **THEN** the system throws InvalidOperationException with descriptive message