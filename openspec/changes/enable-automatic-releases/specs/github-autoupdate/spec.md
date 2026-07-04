## MODIFIED Requirements

### Requirement: Version matches assembly version
The plugin's `Version` property SHALL return the assembly version, which MUST be a 4-part `System.Version`-parsable string (e.g. `1.0.0.0`) matching the `<Version>` in `Directory.Build.props` and the GitHub release tag (with the leading `v` stripped).

#### Scenario: Version is derived from assembly
- **WHEN** ASF queries the plugin's `Version` property
- **THEN** it returns `typeof(CSInventoryPlugin).Assembly.GetName().Version`
- **AND** the value is a 4-part version (e.g. `1.0.0.0`)
- **AND** the value matches the `<Version>` in `Directory.Build.props`

### Requirement: Release artifact is a zip file
Each GitHub release SHALL include a `.zip` file named `CSInventoryPlugin.zip` containing the compiled plugin DLL in the root directory.

#### Scenario: Zip contains plugin DLL at root
- **WHEN** a release is created via CI (the `publish.yml` `release` job, triggered by pushing a `vX.Y.Z.W` tag)
- **THEN** the release asset `CSInventoryPlugin.zip` is attached
- **AND** the zip contains `CSInventoryPlugin.dll` in its root directory

#### Scenario: No zip means no update
- **WHEN** a release exists but has no zip asset
- **THEN** ASF resolves that no update is available for that release

### Requirement: Tags parse to plugin version
Release tags SHALL be in the format `vX.Y.Z.W` where the body after the `v` prefix is a 4-part string parsable to a `System.Version` (e.g. tag `v1.0.0.0` → `1.0.0.0`).

#### Scenario: Tag formatted as vX.Y.Z.W
- **WHEN** a tag `v1.2.3.4` is created on GitHub
- **THEN** ASF parses the version as `1.2.3.4`
- **AND** compares it against the plugin's `Version` property

#### Scenario: Three-part tag is rejected
- **WHEN** a tag `v1.0.0` (3-part) is pushed
- **THEN** the `publish.yml` `release` job's tag/version match check fails
- **AND** no release is created

### Requirement: Plugin binary version matches tag
The DLL produced from a tagged commit SHALL present a `Version` matching the tag's body (e.g. `1.2.3.4` for tag `v1.2.3.4`).

#### Scenario: Built from v1.0.0.0 tag
- **WHEN** CI builds from tag `v1.0.0.0`
- **THEN** `CSInventoryPlugin.dll` reports `Version` as `1.0.0.0`
- **AND** ASF can successfully update from a previous version

## ADDED Requirements

### Requirement: Single release workflow on tag push
The repository SHALL have exactly one GitHub Actions workflow that creates a release on tag push, and that workflow SHALL be `.github/workflows/publish.yml`. No other workflow SHALL create a release on tag push.

#### Scenario: Pushing a v* tag triggers publish.yml release job
- **WHEN** a `v1.0.0.0` tag is pushed to `origin`
- **THEN** the `publish.yml` workflow's `release` job runs
- **AND** it publishes `CSInventoryPlugin.zip` as a non-prerelease GitHub release

#### Scenario: No duplicate release workflow
- **WHEN** a `v1.0.0.0` tag is pushed to `origin`
- **THEN** only one GitHub release is created for that tag
- **AND** no second workflow races to create a release for the same tag
