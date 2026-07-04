## Why

ASF bots currently require manual file manipulation (zipping/unzipping) to enable or disable them. This is error-prone and not user-friendly. We need API endpoints to automate bot state changes and provide status visibility.

## What Changes

- New endpoint to enable a bot by extracting its archived configuration
- New endpoint to disable a bot by archiving its configuration files
- New endpoint to get status of all bots (enabled/disabled)
- Plugin infrastructure for ASF integration

## Capabilities

### New Capabilities
- `bot-management`: Core bot enable/disable/status functionality
- `asf-api-integration`: ASF API client and configuration

### Modified Capabilities
<!-- None -->

## Impact

- New plugin project structure
- ASF API integration dependencies
- Configuration management for ASF folder paths
- File system operations for bot config management