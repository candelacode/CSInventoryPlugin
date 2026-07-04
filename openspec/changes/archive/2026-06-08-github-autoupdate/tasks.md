## 1. Plugin Code Changes

- [x] 1.1 Add `IGitHubPluginUpdates` to `BotManagementPlugin` class declaration
- [x] 1.2 Add `RepositoryName` property returning `"jccan/BotManager.Plugin"`
- [x] 1.3 Add `using ArchiSteamFarm.Plugins.Interfaces` if not already present

## 2. Build Configuration

- [x] 2.1 Add `PostBuild` target to project file to copy DLL to output directory
- [x] 2.2 Add build script (`build.bat`) that produces `BotManager.Plugin.zip` with the DLL

## 3. CI / Release Workflow

- [x] 3.1 Create `.github/workflows/release.yml` that builds on tag push (`v*`)
- [x] 3.2 Extract version from `Directory.Build.props` and verify it matches the tag
- [x] 3.3 Build the project in Release configuration
- [x] 3.4 Create a `.zip` artifact named `BotManager.Plugin.zip` with the DLL
- [x] 3.5 Create a GitHub release and upload the `.zip` as a release asset

## 4. Verification

- [x] 4.1 Verify plugin compiles with `IGitHubPluginUpdates` interface
- [x] 4.2 Verify assembly version matches `Directory.Build.props`
- [ ] 4.3 Verify `RepositoryName` is correctly returned at runtime (requires ASF runtime test)
- [ ] 4.4 Verify CI produces a correctly structured `.zip` asset (requires CI run on tag push)
