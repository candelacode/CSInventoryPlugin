## Context

`BotManagementPlugin` currently implements `IASF` only. ASF supports `IGitHubPluginUpdates`, which enables automatic plugin updates via GitHub releases. The plugin needs to implement this interface, declare its repository name, and ensure proper versioning. The existing `Version` property already returns the assembly version — it just needs to match GitHub release tags.

The ASF plugin template at `JustArchiNET/ASF-PluginTemplate` shows the minimal pattern: implement `IGitHubPluginUpdates`, declare `RepositoryName`, and ensure `Version` returns the assembly version.

## Goals / Non-Goals

**Goals:**
- Implement `IGitHubPluginUpdates` on `BotManagementPlugin`
- Add `RepositoryName` property pointing to `jccan/BotManager.Plugin`
- Ensure assembly version matches GitHub release tags
- Add a build script to produce a release `.zip` with the plugin DLL
- Update `.github/workflows` for automatic tagged releases

**Non-Goals:**
- Custom update logic via `IPluginUpdates` (the GitHub-provided mechanism is sufficient)
- Multi-ASF-version release assets (single `BotManager.Plugin.zip` is fine for now)
- Custom `OnPluginUpdateProceeding` / `OnPluginUpdateFinished` overrides (not needed)

## Decisions

1. **Interface: `IGitHubPluginUpdates` over `IPluginUpdates`** — The GitHub-based mechanism requires zero custom logic: just declare `RepositoryName` and ASF handles the rest. `IPluginUpdates` is only needed for non-GitHub hosting or custom tag-to-version mapping.

2. **Version source: Assembly version** — Already in use (`typeof(BotManagementPlugin).Assembly.GetName().Version`). The `Directory.Build.props` sets `<Version>1.0.0.0</Version>`, which flows into the assembly version. This is the same approach as the official template.

3. **Release artifact: `BotManager.Plugin.zip`** — A single zip containing the compiled DLL placed in the root of the zip. This matches ASF's expectation: it replaces the existing plugin directory contents with the extracted zip contents.

4. **CI: GitHub Actions release workflow** — Use `actions/create-release` and `actions/upload-release-asset` (or `softprops/action-gh-release`) to build, tag, and attach the zip when a version tag is pushed.

5. **Version tag convention: `v<Version>`** — E.g., `v1.0.0.0`. ASF tags matching the `Version` property — the tag format must parse to a `Version` (e.g., `1.0.0.0`). The `v` prefix is human-friendly and is stripped by the template in the tag name used for release.

## Risks / Trade-offs

- **Tag/version mismatch risk** → CI must enforce that the tag pushed matches the `<Version>` in `Directory.Build.props`. Use a workflow step to extract version from the .props file and verify it matches the tag.
- **Missing release zip** → The CI workflow must build the zip and attach it as a release asset. Without it, ASF cannot download the update. Mitigation: make the release workflow the single source of truth for creating releases.
- **`CanUpdate` accidental disable** → Not overriding `CanUpdate` keeps it at default (`true`). If we ever need to disable updates temporarily, we can add the override later.
