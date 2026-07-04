## Why

Users currently must manually download and replace plugin binaries when new versions are released. Enabling `IGitHubPluginUpdates` lets ASF automatically check for, download, and apply updates from GitHub releases — eliminating manual update workflows.

## What Changes

- Implement `IGitHubPluginUpdates` interface on `BotManagementPlugin` (in addition to existing `IASF`)
- Add `RepositoryName` property pointing to `jccan/BotManager.Plugin`
- Ensure `Version` property returns assembly version matching GitHub release tags
- Add build step to create a release `.zip` artifact with plugin binaries
- Configure GitHub Actions (or provide instructions) for CI/CD that creates tagged releases with `.zip` assets

## Capabilities

### New Capabilities
- `github-autoupdate`: Plugin integrates with ASF's `IGitHubPluginUpdates` mechanism — declares its repository name, exposes a parsable version, and produces release artifacts compatible with ASF's update pipeline.

### Modified Capabilities
- `bot-management`: The `BotManagementPlugin` class will implement an additional interface (`IGitHubPluginUpdates`) alongside `IASF`, adding the `RepositoryName` property. No existing behavior changes.

## Impact

- **BotManagementPlugin.cs**: Add `IGitHubPluginUpdates` to class declaration, add `RepositoryName` property
- **Versioning**: Must use assembly version that matches GitHub release tags (e.g., `1.0.0.0`)
- **Build/Release**: A `.zip` release asset containing the plugin DLL must be attached to each GitHub release
- **CI**: GitHub Actions workflow (or manual release process) needs to create versioned tags and releases with `.zip` assets
