## ADDED Requirements

### Requirement: Declare repository for updates
The plugin SHALL implement `IGitHubPluginUpdates` and declare its `RepositoryName` so ASF can locate the GitHub repository for update checks.

#### Scenario: Plugin declares RepositoryName
- **WHEN** ASF inspects the plugin at startup
- **THEN** `RepositoryName` returns `"candelacode/CSInventoryPlugin"`
- **AND** ASF recognizes the plugin as supporting GitHub-based updates

### Requirement: Version matches assembly version
The plugin's `Version` property SHALL return the assembly version, which MUST match the GitHub release tag used for publishing updates.

#### Scenario: Version is derived from assembly
- **WHEN** ASF queries the plugin's `Version` property
- **THEN** it returns `typeof(CSInventoryPlugin).Assembly.GetName().Version`
- **AND** the value matches the `<Version>` in `Directory.Build.props`

### Requirement: Release artifact is a zip file
Each GitHub release SHALL include a `.zip` file named `BotManager.Plugin.zip` containing the compiled plugin DLL in the root directory.

#### Scenario: Zip contains plugin DLL at root
- **WHEN** a release is created via CI
- **THEN** the release asset `CSInventory.Plugin.zip` is attached
- **AND** the zip contains `CSInventory.Plugin.dll` in its root directory

#### Scenario: No zip means no update
- **WHEN** a release exists but has no zip asset
- **THEN** ASF resolves that no update is available for that release

### Requirement: Tags parse to plugin version
Release tags SHALL be in a format parsable to a `System.Version` (e.g., `v1.0.0.0` where the `v` prefix is stripped, yielding `1.0.0.0`).

#### Scenario: Tag formatted as vX.Y.Z.W
- **WHEN** a tag `v1.2.3.4` is created on GitHub
- **THEN** ASF parses the version as `1.2.3.4`
- **AND** compares it against the plugin's `Version` property

### Requirement: Plugin binary version matches tag
The DLL produced from a tagged commit SHALL present a `Version` matching the tag (e.g., `1.2.3.4` for tag `v1.2.3.4`).

#### Scenario: Built from v1.0.1.0 tag
- **WHEN** CI builds from tag `v1.0.1.0`
- **THEN** `CSInventory.Plugin.dll` reports `Version` as `1.0.1.0`
- **AND** ASF can successfully update from a previous version
