## Why

The current `ParseTagAsVersion` strips a leading `v`/`V` but still passes the result directly to `new Version()`, which throws when the tag has fewer than 2 segments (e.g., `v1` → `1`). While ASF's outer `try/catch` prevents a crash, the logged `FormatException` is confusing to users and makes the plugin's update check appear broken. The plugin should be tolerant of short or partial-version tags.

## What Changes

- Make `ParseTagAsVersion` pad short version strings to the minimum 2 segments required by `System.Version`, using `.0` fillers for missing segments (major, minor, build, revision).
- Handle semver-style pre-release suffixes by stripping anything after `-` before parsing.
- The existing `v`/`V` prefix strip is preserved; both enhancements are additive.
- No changes to the update-check flow, repository lookup, or asset selection logic.

## Capabilities

### New Capabilities

### Modified Capabilities
- `github-tag-version-parsing`: The "Strip v prefix before version parsing" and "Unparseable tags surface as no update" requirements are updated — short tags (1-3 segments) after `v`-strip are now padded to valid `System.Version` instead of surfacing as parse errors. Tags like `v1`, `v1.0`, `v1.0.0` become parseable. Pre-release suffixes are stripped. Truly unparseable tags (e.g., `latest`, `release-2024`) still surface errors as before.

## Impact

- `CSInventoryPlugin/CSInventoryPlugin.cs` — update `ParseTagAsVersion` to pad segments and strip suffixes
- `CSInventoryPlugin.Tests/CSInventoryPluginUpdateTests.cs` — update tests for short-tag scenarios, add tests for suffixed tags
- No changes to ASF submodule, GitHub workflows, or public API
