## Why

ASF's default `IGitHubPluginUpdates` implementation parses the GitHub release tag with `new Version(releaseResponse.Tag)`, which throws `FormatException: The input string 'v1' was not in a correct format` whenever a tag starts with a `v` prefix. This silently fails the plugin update check (the exception is swallowed by the outer `try/catch` in `UpdatePlugin`) and contradicts the existing `github-autoupdate` spec, which already states that `v` prefixes MUST be stripped.

## What Changes

- Override the protected `GetTargetReleaseURL(Version, string, bool, bool, bool)` method on `CSInventoryPlugin` so the GitHub release tag has any leading `v`/`V` prefix stripped before being passed to `new Version(...)`.
- Preserve all other behaviors of the default `IGitHubPluginUpdates` implementation (latest-release lookup, asset selection, version comparison logic, logging).
- Stop relying on the broken default implementation in the bundled ASF submodule; the plugin will perform the update check itself using the public `GitHubService.GetLatestRelease` API.
- Add unit tests covering tags with and without a `v` prefix, mixed-case prefixes, empty tags, and tags that still fail to parse.

## Capabilities

### New Capabilities
- `github-tag-version-parsing`: Local override that makes the plugin tolerant of GitHub release tags using the conventional `v`-prefix naming scheme.

### Modified Capabilities
- `github-autoupdate`: The `Tags parse to plugin version` requirement currently describes the behavior as if ASF handles it, but the default implementation does not. The change documents that **the plugin itself** performs the `v`-prefix stripping before parsing.

## Impact

- `CSInventoryPlugin/CSInventoryPlugin.cs` — add a `GetTargetReleaseURL` override and required `using` directives (`ArchiSteamFarm.Web.GitHub`, `ArchiSteamFarm.Web.GitHub.Data`).
- `CSInventoryPlugin.Tests/` — add tests verifying the override.
- Bundled ASF submodule (`ArchiSteamFarm/`) — **unchanged**. The fix is local to the plugin.
- No public API or wire-protocol changes. Behavior change is only visible during update checks for repos whose tags use the `v` prefix.
