## Context

The plugin targets `net10.0` and lives in this repo (CSInventory.Plugin) with the ASF source tree as a submodule. The plugin assembly is built from `CSInventoryPlugin/CSInventoryPlugin.csproj`, which produces an assembly named `CSInventoryPlugin.dll` (the project name, no dot). The plugin class already implements `IGitHubPluginUpdates` with `RepositoryName => "candelacode/CSInventoryPlugin"`, so ASF can locate the GitHub repo for update checks; the rest of the IGitHubPluginUpdates contract (version, tag, zip asset) is what we are now wiring up so the very first release is genuinely auto-updatable from an end-user's ASF.

Today the pieces are partially in place but do not fit together:

- `Directory.Build.props` has `<Version>1.0.0</Version>` (3 parts). The `github-autoupdate` spec requires a 4-part `System.Version`-parsable string. `dotnet publish` propagates `<Version>` into `AssemblyVersion`/`AssemblyFileVersion`/`Version` (via the `Version` property read in `CSInventoryPlugin.cs:31`), so the assembly currently reports `1.0.0` (which `System.Version` accepts as `1.0.0.0` only at parse time, not at format time). For a tag `v1.0.0.0` to match, the props version must be the literal `1.0.0.0`.
- The `github-autoupdate` spec currently says release assets are named `CSInventory.Plugin.zip` and contain `CSInventory.Plugin.dll`. Nothing in this repo produces those names — the project, the assembly, and the convention used by `publish.yml` (which reads `PluginName` from `Directory.Build.props`, i.e. `CSInventoryPlugin`) all use the dotless name. The spec must be corrected to match the build.
- The repo has two release workflows: `.github/workflows/publish.yml` (the ASF-PluginTemplate standard, cross-platform, GPG signing, separate `release` job that runs only on tag push) and `.github/workflows/release.yml` (a hand-rolled release-only workflow that hardcodes `CSInventory.Plugin` and would `dotnet publish` a non-existent project). On a tag push, both would fire, and `release.yml` would fail.
- A local tag `v1.0.0` exists from a prior attempt but it was never pushed to `origin` and never produced a GitHub release. The `v1.0.0` tag has been deleted locally as part of preparing this change.

The plugin's `Version` property already derives from the assembly version, so once the props version is `1.0.0.0` and the assembly is built from that source, ASF will read `1.0.0.0` with no code change.

## Goals / Non-Goals

**Goals:**

- Make the first release (`v1.0.0.0`) a real, auto-updatable release: pushing the tag produces a GitHub release with `CSInventoryPlugin.zip` containing `CSInventoryPlugin.dll`, whose `AssemblyVersion` is `1.0.0.0`.
- Bring the `github-autoupdate` spec into alignment with the real project name (`CSInventoryPlugin`, not `CSInventory.Plugin`) and the real version format (4-part).
- Consolidate to a single release-creating workflow (`.github/workflows/publish.yml`) so tag pushes cannot race.
- Set the `publish.yml` `release` job to publish a stable (non-prerelease) release so the v1.0.0.0 cut is a real GA.
- Document the release procedure in the README so the next maintainer (or future me) can cut a release by following steps, not by reading the YAML.

**Non-Goals:**

- No source code change to `CSInventoryPlugin/*.cs`. The `Version` property already reads from `Assembly.GetName().Version`, and that is wired up correctly.
- No change to the `IGitHubPluginUpdates` interface contract or to `RepositoryName`.
- No introduction of GPG signing for SHA512SUMS — that already exists optionally in `publish.yml` (gated on `ARCHIBOT_GPG_PRIVATE_KEY`); we are not enabling it as part of this change.
- No change to user-facing ASF config (`PluginsUpdateMode`, `PluginsUpdateList`) — the README will mention it, but the plugin does not own that config.
- No NuGet publishing, no `dotnet pack` — this is plugin release, not package release.

## Decisions

### Decision 1: Use `publish.yml` as the single release workflow; delete `release.yml`

`publish.yml` (the ASF-PluginTemplate standard) already does everything `release.yml` does and more: cross-platform build matrix, GPG-signed SHA512SUMS, conditional compression level (fast for PRs, max for tags), and a separate `release` job that only runs on tag push. `release.yml` is a hand-rolled duplicate that hardcodes `CSInventory.Plugin` (a project that does not exist) and would fail at `dotnet publish "$PLUGIN_NAME"`. Keeping both is strictly worse: on a tag push both fire, and one fails. Deleting `release.yml` and flipping `publish.yml`'s `release` job to `prerelease: false` gives us a single, correct, cross-platform release pipeline.

**Alternatives considered:**

- *Keep `release.yml` and fix it (replace `CSInventory.Plugin` with `CSInventoryPlugin`, set the correct version, etc.) instead of deleting it.* Rejected: it is a strict subset of what `publish.yml` does. Maintaining two release workflows in one repo adds nothing and doubles the surface area for future breakage (e.g. a future contributor will inevitably patch one and forget the other).
- *Keep both but guard `release.yml` with `if: false`.* Rejected: dead code is worse than no code, and someone will eventually uncomment the guard.
- *Use `release.yml` and disable `publish.yml`'s `release` job.* Rejected: that would force the project off the ASF-PluginTemplate standard and lose the cross-platform build matrix / GPG signing / SHA512SUMS.

### Decision 2: Bump the version literal to `1.0.0.0` (4-part) in `Directory.Build.props`

The `github-autoupdate` spec requires tags to be `vX.Y.Z.W` and the assembly version to match. The CI's `release` job, when it exists in `publish.yml`, would compare the tag's body to `<Version>` in `Directory.Build.props`; the comparison is string equality, so the literal must be `1.0.0.0`. `System.Version` parses both `1.0.0` and `1.0.0.0` to the same value, but `1.0.0.0.ToString()` yields `1.0.0.0`, and that is what ASF and the spec expect. Bumping the literal in the props is the single source of truth — `CSInventoryPlugin.cs`'s `Version => typeof(CSInventoryPlugin).Assembly.GetName().Version` already picks it up.

**Alternatives considered:**

- *Set `<Version>1.0.0</Version>` and rely on `System.Version` to normalize.* Rejected: the spec requires 4-part, and ASF's tag/version comparison in `publish.yml` is string-based. Any future contributor who tries to cut a `v1.0.0.1` patch release and sets `<Version>1.0.0.1</Version>` in the props will get a release; the 3-part format is not the failure mode, but normalizing now to 4-part avoids ambiguity forever.
- *Add a separate `<AssemblyVersion>` / `<FileVersion>` property instead of changing `<Version>`.* Rejected: ASF reads the assembly's `Version`, and `dotnet` propagates `<Version>` into it by default. Adding a separate property is unnecessary indirection.

### Decision 3: Correct the `github-autoupdate` spec to use the real project name

The spec's references to `CSInventory.Plugin.zip` and `CSInventory.Plugin.dll` are aspirational and do not match the build. The fix is to rewrite those three requirement blocks (`Release artifact is a zip file`, `Tags parse to plugin version`, `Plugin binary version matches tag`) to use the real name `CSInventoryPlugin` and the 4-part version. We are also adding a new requirement `Single release workflow on tag push` to lock in the consolidation of `publish.yml` and the removal of `release.yml`, so a future contributor does not reintroduce the duplicate workflow.

**Alternatives considered:**

- *Rename the project to `CSInventory.Plugin` (with a dot) to match the spec.* Rejected: this is gratuitous churn (rename all files, namespaces, GitHub repo name, project URL) for no real benefit. `CSInventoryPlugin` is the established name (used in the repo URL, the README, `Directory.Build.props`' `PluginName`, the assembly name, the test project name). The spec was the thing that was wrong, not the name.
- *Keep the spec vague ("a .zip with the plugin DLL") instead of naming names.* Rejected: the spec is supposed to be the contract that the build must satisfy. Vague wording makes it impossible to test whether the build is doing the right thing.

### Decision 4: Document the release process in the README

The release flow is currently discoverable only by reading the CI YAML. Adding a short "Releasing" section to the README (bump version in `Directory.Build.props`, commit, `git tag vX.Y.Z.W`, `git push origin vX.Y.Z.W`) means the next maintainer does not need to read CI to know how to cut a release. We also mention the user-side `PluginsUpdateMode` and `PluginsUpdateList` config so users reading the README know what to set on their ASF to receive the auto-update.

**Alternatives considered:**

- *Put the release process in `CONTRIBUTING.md` instead of the README.* Rejected: the release process is more of an ops/maintainer concern than a contributing concern, and the README is the canonical place readers look first. We will add a brief note; `CONTRIBUTING.md` can cross-reference it.
- *Add a `RELEASING.md` at the repo root.* Rejected: that's a third file for the same information; one place is enough.

## Risks / Trade-offs

- **Risk:** Deleting `.github/workflows/release.yml` removes the only workflow that today enforces the tag/version match (`Verify tag matches plugin version` step). After deletion, nothing in `publish.yml` checks that the tag matches the props version. → **Mitigation:** Either (a) port that check into the `publish.yml` `release` job (one `if:`-guarded step is enough), or (b) accept the risk because `publish.yml` always builds from the commit pointed at by the tag, and the props version is the same commit's source of truth, so the version baked into the DLL will match the tag body by construction. We will go with (b) for now (the change scope is "enable automatic releases", not "tighten CI checks") and document the trade-off in the README — the `release` job's `prerelease: false` setting plus the dotless name plus the 4-part version requirement in the spec is enough friction that a bad tag will be obvious on inspection. If a future contributor wants the check back, it is a one-step addition.

- **Risk:** Setting `prerelease: false` in `publish.yml`'s `release` job makes the very next tag push (`v1.0.0.0`) a non-prerelease GitHub release, which becomes the "latest" release. If the plugin has any pre-release-only blockers we have not noticed, this will publish a bad version as `latest`. → **Mitigation:** This change ships in the same commit that becomes `v1.0.0.0`. The pre-release cleanup has already been done (legacy `sendcsitems` removed, namespace/folder mismatch fixed, NU1903 suppressed). If anything else is found, we cut `v1.0.0.0` as a prerelease instead (`prerelease: true`) — flipping one line — and flip it to `false` for the first GA. The spec's "Single release workflow on tag push" requirement does not pin the prerelease flag, so this is a fine-tuning decision we can reverse in 30 seconds.

- **Risk:** The local `v1.0.0` tag was deleted; if the user had pushed it to a fork or another remote, that remote would still have a `v1.0.0` reference. → **Mitigation:** The repo has only `origin` (`https://github.com/candelacode/CSInventoryPlugin.git`), and `origin` has no `v1.0.0` tag (confirmed via `git ls-remote` and the GitHub API returning an empty tags list). No remote cleanup is needed.

- **Risk:** The `github-autoupdate` spec delta rewrites the artifact-naming requirements. If a third party (e.g. a downstream plugin) depended on the old `CSInventory.Plugin.zip` name, they break. → **Mitigation:** Nothing downstream can depend on this name — `CSInventory.Plugin.zip` does not exist (the build never produced it). This is a no-op in practice.

- **Trade-off:** The "Single release workflow on tag push" requirement is a constraint on repo files, not on the plugin itself. If a future contributor adds a *new* workflow that creates a release on tag push, they will have to update the spec. This is the right kind of friction — adding a duplicate release workflow should require an OpenSpec change, not a silent YAML edit.

## Migration Plan

This is a small change; the migration is the release itself.

1. Land this change on `main` (the props version bump, the spec rewrite, the `publish.yml` `prerelease: false` flip, the deletion of `release.yml`, the README addition). The existing `IGitHubPluginUpdates` is unchanged; no user-visible behavior changes for users on previous unreleased builds.
2. Tag the merge commit as `v1.0.0.0` and push the tag: `git tag v1.0.0.0 && git push origin v1.0.0.0`.
3. The `publish.yml` `release` job runs, builds the plugin on `ubuntu-latest`, macOS, and Windows, zips the output, generates SHA512SUMS, creates the GitHub release, and attaches `CSInventoryPlugin.zip` (plus the Windows-only `CSInventoryPlugin.zip` from the Windows matrix, etc.).
4. Verify on GitHub that the release shows up as a non-prerelease, "latest" release, and that `CSInventoryPlugin.zip` is attached.
5. **Rollback:** If something goes wrong, delete the GitHub release and the remote tag (`git push origin :refs/tags/v1.0.0.0`), revert this commit on `main`, and re-cut from a fixed tree. The local tag can be re-created trivially. No user has the new build yet, so there is nothing to roll back from the user side.

## Open Questions

- None. The change is small enough that all decisions are resolved; the only thing that remains is the actual cut of `v1.0.0.0` after this change is merged, which is documented in the migration plan and the README.
