## Why

The plugin already implements `IGitHubPluginUpdates` and ships a release workflow, but the setup is not actually end-to-end releasable: (1) the `<Version>` in `Directory.Build.props` is the 3-part string `1.0.0` while the `github-autoupdate` spec requires a 4-part `System.Version`-parsable value (`1.0.0.0`), so a tag `v1.0.0.0` would fail the workflow's tag/version match check; (2) the `github-autoupdate` spec references `CSInventory.Plugin.zip` and `CSInventory.Plugin.dll`, but the actual project/assembly is `CSInventoryPlugin` (no dot), so the spec describes artifacts that no part of the build produces; (3) the repo has two release workflows (`.github/workflows/publish.yml` and `.github/workflows/release.yml`) that would both fire on a tag push, with `release.yml` hardcoding the wrong `CSInventory.Plugin` project name; (4) no local tag exists that matches the spec'd format, so a clean 1.0.0.0 release is not yet producible. We need to make the spec match the build output, the version match the spec, and the release pipeline a single, correct workflow, then re-publish from a 4-part tag.

## What Changes

- **Bump `<Version>` in `Directory.Build.props` from `1.0.0` to `1.0.0.0`** so the assembly version is parsable as a `System.Version` and matches the spec'd tag format. (Already applied in the working tree as part of preparing this change.)
- **Delete the local `v1.0.0` tag** — it points at a commit whose build artifacts are 3-part and whose `Directory.Build.props` still reads `1.0.0`. It is not on `origin` and no GitHub release was ever created from it, so no remote cleanup is needed. (Already done.)
- **Update the `github-autoupdate` spec** so all artifact/DLL references use the real project name `CSInventoryPlugin` (no dot) — i.e. release asset `CSInventoryPlugin.zip` containing `CSInventoryPlugin.dll` at the root. The spec's "Version matches assembly version" requirement is updated to expect `1.0.0.0` (4-part) rather than just "matches `<Version>` in `Directory.Build.props`" without specifying parts.
- **Consolidate the release pipeline to a single workflow.** `publish.yml` (the ASF-PluginTemplate standard) already builds the plugin on push/PR and creates a GitHub release on tag push (with cross-platform zips, optional GPG-signed SHA512SUMS). `.github/workflows/release.yml` is a hand-rolled duplicate that hardcodes `CSInventory.Plugin` and would fail. We will:
  - In `publish.yml`, switch the `release` job to publish a non-prerelease stable release (set `prerelease: false` instead of `prerelease: true`) for our `vX.Y.Z.W` tags, so the first 1.0.0.0 release lands as a stable release.
  - Delete `.github/workflows/release.yml` so the two workflows cannot race on a tag push.
- **Document the release process** in the README: how to bump the version in `Directory.Build.props`, create the `vX.Y.Z.W` tag, push it, and how ASF picks up the update via `IGitHubPluginUpdates` (with the `PluginsUpdateMode` / `PluginsUpdateList` config values users must set).

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `github-autoupdate`: Correct the DLL and zip name references from the placeholder `CSInventory.Plugin.*` to the real `CSInventoryPlugin.*`. Tighten the version requirement to be 4-part (`System.Version`-parsable) and explicitly mention that the `<Version>` in `Directory.Build.props` MUST be a 4-part string (e.g. `1.0.0.0`). Add a scenario covering that the tag pushed to GitHub drives the `publish.yml` `release` job and the published asset is `CSInventoryPlugin.zip`.

## Impact

- **Build config**:
  - `Directory.Build.props` — `<Version>` changes from `1.0.0` to `1.0.0.0`. (Already applied.)
- **Source**: no changes to `CSInventoryPlugin/*.cs` — the `Version` property already reads from `typeof(CSInventoryPlugin).Assembly.GetName().Version`, so the plugin will report `1.0.0.0` automatically after the props bump.
- **CI / workflows**:
  - `.github/workflows/publish.yml` — change the `release` job's `prerelease:` from `true` to `false` so v1.x tags produce a non-prerelease release.
  - `.github/workflows/release.yml` — deleted (its functionality is fully covered by `publish.yml`, and it contained a hardcoded `CSInventory.Plugin` project name that doesn't exist in this repo).
- **Git / releases**:
  - Local `v1.0.0` tag deleted (it was never pushed, no GitHub release was ever created from it).
  - The new release is created by pushing a `v1.0.0.0` tag after this change is merged.
- **Docs**:
  - `README.md` — add a short "Releasing" section explaining: (a) bump `<Version>` in `Directory.Build.props` to the new 4-part value, (b) commit, (c) `git tag vX.Y.Z.W` and `git push origin vX.Y.Z.W`, (d) the `publish.yml` `release` job creates the GitHub release with `CSInventoryPlugin.zip` attached. Mention the user-side `PluginsUpdateMode` / `PluginsUpdateList` config values required for ASF to auto-update.
- **Spec**:
  - `openspec/specs/github-autoupdate/spec.md` — rewrite the three "name"-bearing requirements (`Release artifact is a zip file`, `Tags parse to plugin version`, `Plugin binary version matches tag`) to use the real names `CSInventoryPlugin.zip` / `CSInventoryPlugin.dll` and a 4-part version.
- **Users / runtime behavior**: No behavior change for end users on this change itself. After the `v1.0.0.0` release is published, users with `PluginsUpdateMode: 3` and this plugin in `PluginsUpdateList` will receive the first auto-updatable build.
