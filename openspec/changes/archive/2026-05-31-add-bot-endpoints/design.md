## Context

ASF (ArchiSteamFarm) manages Steam bots through configuration files in a config directory. Currently, enabling/disabling bots requires manual file operations (zipping/unzipping). The existing AsfClientService provides a reference implementation for these operations.

This plugin will expose HTTP API endpoints to automate bot management, following ASF's plugin architecture.

## Goals / Non-Goals

**Goals:**
- Provide REST API endpoints for bot enable/disable/status operations
- Reuse existing ASF configuration patterns and file structure
- Integrate with ASF's plugin system and logging
- Maintain compatibility with existing ASF configurations

**Non-Goals:**
- Modify ASF core functionality
- Support bot creation/deletion via API
- Provide web UI for bot management
- Handle bot authentication/authorization beyond ASF's built-in security

## Decisions

**1. Use ASP.NET Core Minimal APIs**
- Rationale: ASF already uses ASP.NET Core for its web interface
- Alternatives considered: Custom HTTP handler (rejected - would duplicate web server logic)

**2. Leverage existing AsfClientService patterns**
- Rationale: Proven zip/unzip logic for bot state changes
- Implementation: Extract reusable logic into plugin services

**3. Configuration via ASF's options pattern**
- Rationale: Consistent with ASF's configuration approach
- Use Microsoft.Extensions.Options for ASF folder paths and API URLs

**4. Plugin implements IAspNetCoreConfiguration**
- Rationale: Standard ASF plugin pattern for web API endpoints
- Allows registering routes in ASF's web pipeline

## Risks / Trade-offs

**File system operations** → Could cause race conditions with ASF's bot management
- Mitigation: Use file locking and atomic operations where possible

**Configuration drift** → Manual changes to config directory could desync state
- Mitigation: Refresh bot list on each API call, don't cache

**ASF version compatibility** → ASF updates may change plugin interfaces
- Mitigation: Target latest ASF version, document compatibility