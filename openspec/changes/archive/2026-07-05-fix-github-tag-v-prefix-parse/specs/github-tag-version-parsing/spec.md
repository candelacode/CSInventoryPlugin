## ADDED Requirements

### Requirement: Strip v prefix before version parsing
The plugin SHALL strip a leading `v` or `V` from a GitHub release tag before constructing a `System.Version` from it, so that tags using the conventional `vX.Y.Z.W` naming scheme parse successfully.

#### Scenario: Tag with lowercase v prefix
- **WHEN** the GitHub release tag is `v1.2.3.4`
- **THEN** the plugin parses the version as `1.2.3.4` and proceeds with the update check

#### Scenario: Tag with uppercase V prefix
- **WHEN** the GitHub release tag is `V1.2.3.4`
- **THEN** the plugin parses the version as `1.2.3.4` and proceeds with the update check

#### Scenario: Tag with single-character v prefix
- **WHEN** the GitHub release tag is `v1`
- **THEN** the plugin attempts to parse the version by first stripping the leading `v`
- **AND** surfaces a parse error to ASF's outer `UpdatePlugin` flow, which logs it and resolves the update as "no update available"

#### Scenario: Tag with no prefix
- **WHEN** the GitHub release tag is `1.2.3.4` (no `v` prefix)
- **THEN** the plugin parses the version as `1.2.3.4` and proceeds with the update check

### Requirement: Unparseable tags surface as no update
The plugin SHALL treat tags that still cannot be parsed as `System.Version` after the `v`/`V` strip (e.g., `release-2024`, `latest`, or `v1` because `System.Version` requires 2-4 segments) as "no update available" rather than throwing an unhandled exception, by relying on ASF's outer error handling in `UpdatePlugin`.

#### Scenario: Tag with non-numeric content
- **WHEN** the GitHub release tag is `latest`
- **THEN** the plugin surfaces a parse error (`ArgumentException` from `System.Version`) to ASF's outer flow
- **AND** ASF logs the exception and the update check for that release resolves to "no update available"

### Requirement: Reuse GitHubService for release lookup
The plugin SHALL use the public `ArchiSteamFarm.Web.GitHub.GitHubService.GetLatestRelease` API to fetch the latest release, the same API used by ASF's default `IGitHubPluginUpdates` implementation, so that behavior remains consistent with the upstream flow.

#### Scenario: Latest release is fetched via GitHubService
- **WHEN** the plugin performs an update check
- **THEN** it calls `GitHubService.GetLatestRelease(RepositoryName, stable)` with the same `RepositoryName` and `stable` flag ASF would have used
- **AND** it does not perform any other network calls during the lookup
