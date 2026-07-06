## 1. Implementation

- [x] 1.1 In `CSInventoryPlugin/CSInventoryPlugin.cs`, add `using` directives for `ArchiSteamFarm.Localization`, `ArchiSteamFarm.Web.GitHub`, and `ArchiSteamFarm.Web.GitHub.Data`.
- [x] 1.2 Implement `Task<Uri?> IPluginUpdates.GetTargetReleaseURL(Version asfVersion, string asfVariant, bool asfUpdate, GlobalConfig.EUpdateChannel updateChannel, bool forced)` explicitly on `CSInventoryPlugin` so the runtime dispatches to our version instead of the sealed default interface method in `IGitHubPluginUpdates`.
- [x] 1.3 Inside the explicit implementation, mirror the body of `IGitHubPluginUpdates.GetTargetReleaseURL(...)`: keep the `CanUpdate` short-circuit, the `RepositoryName` validation, the `GitHubService.GetLatestRelease` lookup, the `GetTargetReleaseAsset` call, and the `Strings.FormatPluginUpdate*` log calls identical to the upstream default. The `GetPossibleNames` body is inlined because the helper is a `private` sealed DIM that the class cannot call.
- [x] 1.4 Strip a leading `v`/`V` from `releaseResponse.Tag` before constructing `new Version(...)`, via an `internal static Version ParseTagAsVersion(string tag)` helper that is also exercised by the unit tests.
- [x] 1.5 Add a `// Mirrors ArchiSteamFarm/.../IGitHubPluginUpdates.cs:60 and :154` comment on the explicit interface implementation to keep both copies in sync.

## 2. Tests

- [x] 2.1 Add a new test class `CSInventoryPluginUpdateTests` to `CSInventoryPlugin.Tests/` covering the `ParseTagAsVersion` helper.
- [x] 2.2 Test that a tag of `v1.2.3.4` produces a `Version` of `1.2.3.4`.
- [x] 2.3 Test that an uppercase `V1.2.3.4` also strips correctly.
- [x] 2.4 Test that a `v1` (single segment) tag propagates an `ArgumentException` from `System.Version` (because `new Version("1")` is invalid) — this is the exception the outer `UpdatePlugin` flow will log and treat as "no update available".
- [x] 2.5 Test that a tag of `1.2.3.4` (no prefix) still parses to `1.2.3.4`.
- [x] 2.6 Test that a tag of `latest` (unparseable) throws `ArgumentException` — this is the exception the outer `UpdatePlugin` flow will log and treat as "no update available".

## 3. Build and verify

- [x] 3.1 Run `dotnet build CSInventoryPlugin.slnx` and confirm the plugin compiles with no new warnings.
- [x] 3.2 Run `dotnet test CSInventoryPlugin.Tests/` and confirm all new and existing tests pass.
- [x] 3.3 Run `openspec validate fix-github-tag-v-prefix-parse --strict` and confirm the change passes.
