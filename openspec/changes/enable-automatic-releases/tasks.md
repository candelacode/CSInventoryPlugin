## 1. Bump version in Directory.Build.props

- [x] 1.1 In `Directory.Build.props`, change `<Version>1.0.0</Version>` to `<Version>1.0.0.0</Version>` (4-part, `System.Version`-parsable, matches the spec'd tag format `v1.0.0.0`).

## 2. Delete the obsolete local tag

- [x] 2.1 Delete the local `v1.0.0` tag (`git tag -d v1.0.0`). It was never pushed to `origin` and never produced a GitHub release, so no remote cleanup is required. Confirmed via `git ls-remote` and the GitHub API: no `v1.0.0` tag and no release exist on `candelacode/CSInventoryPlugin`.

## 3. Consolidate the release pipeline to a single workflow

- [x] 3.1 In `.github/workflows/publish.yml`, change the `release` job's `prerelease:` input from `prerelease: true` to `prerelease: false` so that pushing a `vX.Y.Z.W` tag produces a stable (non-prerelease) GitHub release that becomes the project's "latest" release.
- [x] 3.2 Delete `.github/workflows/release.yml` (its functionality is fully covered by `publish.yml`, and it contained a hardcoded `CSInventory.Plugin` project name that does not exist in this repo).

## 4. Update the github-autoupdate spec

- [x] 4.1 The `openspec/changes/enable-automatic-releases/specs/github-autoupdate/spec.md` delta rewrites the three name-bearing requirements to use the real project name `CSInventoryPlugin` (no dot) and tightens the version requirement to 4-part. **Note:** the canonical spec at `openspec/specs/github-autoupdate/spec.md` is intentionally NOT touched during apply — the OpenSpec archive step applies the delta to the canonical. Touching both would double-apply.

## 5. Document the release process in the README

- [x] 5.1 In `README.md`, add a "Releasing" section that walks a maintainer through: (a) bumping `<Version>` in `Directory.Build.props` to the new 4-part value, (b) committing, (c) `git tag vX.Y.Z.W && git push origin vX.Y.Z.W`, (d) the `publish.yml` `release` job then publishes `CSInventoryPlugin.zip` as a GitHub release. Note that ASF will pick up the update for users who have `PluginsUpdateMode: 3` and this plugin in `PluginsUpdateList`. (Also added a "Receiving automatic updates (user-side)" subsection showing the `PluginsUpdateMode: 3` / `PluginsUpdateList.CSInventoryPlugin: true` config snippet.)

## 6. Verify

- [x] 6.1 Run `dotnet build -c Release` and confirm the build succeeds with the new `<Version>1.0.0.0</Version>`. Inspect the produced `CSInventoryPlugin.dll` with `dotnet --info`/assembly metadata and confirm `Version` is `1.0.0.0`. (Build succeeded — 0 errors, 0 NU1903 from our projects. `[System.Reflection.AssemblyName]::GetAssemblyName(...).Version` on the built DLL reports `1.0.0.0`.)
- [x] 6.2 Run `dotnet test` and confirm all tests pass (no source change expected, but sanity check). (19/19 pass, 0 failed.)
- [x] 6.3 Run `openspec validate "enable-automatic-releases" --type change` and confirm the change validates cleanly. (`Change 'enable-automatic-releases' is valid`.)
- [ ] 6.4 Commit the changes (props bump, `publish.yml` `prerelease: false` flip, `release.yml` deletion, README addition, no `v*` tag) and push to `origin/main`. **Requires user action — I do not commit/push without explicit go-ahead.**
- [ ] 6.5 After merge, create the `v1.0.0.0` tag on the merge commit and push it: `git tag v1.0.0.0 && git push origin v1.0.0.0`. **Requires user action.**
- [ ] 6.6 Watch the `publish.yml` `release` job on GitHub Actions and confirm it (a) builds the plugin on `ubuntu-latest`/macOS/Windows, (b) attaches `CSInventoryPlugin.zip` to a non-prerelease GitHub release, (c) the release is marked as the project's "latest" release. **Requires user action.**
- [ ] 6.7 Sanity-check the published `CSInventoryPlugin.zip` by downloading it and confirming the top-level entry is `CSInventoryPlugin.dll` (not `CSInventory.Plugin.dll` and not nested in a folder). **Requires user action.**
