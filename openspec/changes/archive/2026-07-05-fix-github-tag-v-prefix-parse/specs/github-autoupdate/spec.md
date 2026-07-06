## MODIFIED Requirements

### Requirement: Tags parse to plugin version
Release tags SHALL be in a format parsable to a `System.Version` (e.g., `v1.0.0.0` where the `v` prefix is stripped, yielding `1.0.0.0`). The plugin SHALL perform the `v`/`V` prefix stripping itself in its overridden `GetTargetReleaseURL`, because the default `IGitHubPluginUpdates` implementation in the bundled ASF build does not handle the `v` prefix and would otherwise throw `FormatException`.

#### Scenario: Tag formatted as vX.Y.Z.W
- **WHEN** a tag `v1.2.3.4` is created on GitHub
- **THEN** the plugin strips the leading `v` and parses the version as `1.2.3.4`
- **AND** compares it against the plugin's `Version` property

#### Scenario: Tag without v prefix still works
- **WHEN** a tag `1.2.3.4` (no `v` prefix) is created on GitHub
- **THEN** the plugin parses the version as `1.2.3.4` without modification
- **AND** compares it against the plugin's `Version` property
