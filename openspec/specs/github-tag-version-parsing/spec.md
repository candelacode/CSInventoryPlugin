## ADDED Requirements

### Requirement: Strip v prefix before version parsing
The plugin SHALL strip a leading `v` or `V` from a GitHub release tag, strip any `-` suffix (pre-release identifier), and pad the remaining numeric segments with `.0` to produce a valid 4-segment `System.Version`.

#### Scenario: Tag with lowercase v prefix
- **WHEN** the GitHub release tag is `v1.2.3.4`
- **THEN** the plugin parses the version as `1.2.3.4` and proceeds with the update check

#### Scenario: Tag with uppercase V prefix
- **WHEN** the GitHub release tag is `V1.2.3.4`
- **THEN** the plugin parses the version as `1.2.3.4` and proceeds with the update check

#### Scenario: Single-segment tag with v prefix
- **WHEN** the GitHub release tag is `v1`
- **THEN** the plugin strips `v`, pads to 4 segments, and parses as `1.0.0.0`

#### Scenario: Two-segment tag with v prefix
- **WHEN** the GitHub release tag is `v1.0`
- **THEN** the plugin strips `v`, pads to 4 segments, and parses as `1.0.0.0`

#### Scenario: Three-segment tag with v prefix
- **WHEN** the GitHub release tag is `v1.0.0`
- **THEN** the plugin strips `v`, pads to 4 segments, and parses as `1.0.0.0`

#### Scenario: Tag with pre-release suffix
- **WHEN** the GitHub release tag is `v1.2.3.4-beta`
- **THEN** the plugin strips `v`, strips the `-beta` suffix, and parses as `1.2.3.4`

#### Scenario: Tag with no prefix
- **WHEN** the GitHub release tag is `1.2.3.4` (no `v` prefix)
- **THEN** the plugin parses the version as `1.2.3.4` and proceeds with the update check

### Requirement: Unparseable tags surface as no update
The plugin SHALL treat tags that still cannot be parsed as `System.Version` after `v`/`V` strip, `-` suffix strip, and segment padding (e.g., `release-2024`, `latest`, or tags with non-numeric segments) as "no update available" by relying on ASF's outer error handling in `UpdatePlugin`.

#### Scenario: Tag with non-numeric content
- **WHEN** the GitHub release tag is `latest`
- **THEN** the plugin surfaces a parse error (`FormatException` from `System.Version`) to ASF's outer flow
- **AND** ASF logs the exception and the update check for that release resolves to "no update available"

#### Scenario: Tag with non-numeric segment after strip and pad
- **WHEN** the GitHub release tag is `vone.two`
- **THEN** the plugin attempts to parse and surfaces a parse error to ASF's outer flow
- **AND** ASF logs the exception and the update check for that release resolves to "no update available"

### Requirement: Reuse GitHubService for release lookup
The plugin SHALL use the public `ArchiSteamFarm.Web.GitHub.GitHubService.GetLatestRelease` API to fetch the latest release, the same API used by ASF's default `IGitHubPluginUpdates` implementation, so that behavior remains consistent with the upstream flow.

#### Scenario: Latest release is fetched via GitHubService
- **WHEN** the plugin performs an update check
- **THEN** it calls `GitHubService.GetLatestRelease(RepositoryName, stable)` with the same `RepositoryName` and `stable` flag ASF would have used
- **AND** it does not perform any other network calls during the lookup
