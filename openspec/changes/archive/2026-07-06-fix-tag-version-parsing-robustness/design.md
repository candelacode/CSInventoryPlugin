## Context

The existing `ParseTagAsVersion` method strips a leading `v`/`V` prefix from GitHub release tags (per the `github-tag-version-parsing` spec) but then passes the result directly to `new Version()`. `System.Version` requires at least 2 dot-separated segments (`major.minor`). Tags like `v1` produce `"1"` after stripping, which throws.

The current test suite explicitly expects `v1` to throw an `ArgumentException`. The user reports a `FormatException` for `v1`, confirming the gap: short-segment tags break the update check.

### Current `ParseTagAsVersion`

```csharp
internal static Version ParseTagAsVersion(string tag) {
    if (!string.IsNullOrEmpty(tag) && ((tag[0] == 'v') || (tag[0] == 'V'))) {
        tag = tag.Substring(1);
    }
    return new Version(tag);
}
```

## Goals / Non-Goals

**Goals:**
- Make `ParseTagAsVersion` tolerate tags with 1-3 segments by zero-padding missing segments
- Strip semver-style pre-release suffixes (`-beta`, `-rc1`, etc.) before parsing
- Preserve existing `v`/`V` prefix stripping

**Non-Goals:**
- Semantic version comparison (e.g., evaluating pre-release precedence)
- Changing the update-check flow, repository lookup, or asset selection
- Handling arbitrary non-numeric tag formats (those continue to throw)

## Decisions

| Decision | Rationale |
|---|---|
| **Zero-pad to 4 segments** | `System.Version` needs at least 2 segments. Padding to 4 matches the .NET convention used in `Directory.Build.props` (`1.0.0.0`) and ASF's own `Version` property. A 2-segment `1.0` → `1.0.0.0` cleanly preserves the major/minor intent. |
| **Strip `-` suffix, not regex-match semver** | Most real-world pre-release tags use `-` (e.g., `v1.0.0-beta`). A simple `IndexOf('-')` substring avoids dependency on semver parsing libraries and handles ad hoc suffixes. |
| **Split on `.`, pad, rejoin** | `tag.Split('.')` yields the numeric segment parts. Appending `".0"` for missing segments is simpler and more readable than try/catch retry logic. |
| **Do not catch parse errors in the helper** | Matching the existing design: unparseable tags (non-numeric after strip/pad, e.g., `latest`, `release-2024`) still throw, and ASF's outer `try/catch` in `PluginsCore.UpdatePlugin` logs the exception as "no update available." This is a safe failure mode. |

### Alternatives considered

- **TryParse with fallback**: `Version.TryParse` + fallback to `0.0.0.0` would silently mask tag typos. Better to surface clear errors.
- **Pad only to 2 segments**: Works for `v1` → `1.0` but doesn't align with the 4-segment convention used by `Version` and the build system.
- **Set `Version.Major`/`Minor` properties directly**: `System.Version` is immutable; can't be constructed segment-by-segment.

## Risks / Trade-offs

- **[Risk] Zero-padding could cause unintended version comparisons** (e.g., `v2` → `2.0.0.0` correctly compares against `1.0.0.0`). Mitigation: This is the intended behavior — the padding is predictable and preserves the numeric ordering.
- **[Risk] Suffix stripping loses pre-release semantics** (e.g., `v1.0.0-beta` vs `v1.0.0` are treated identically). Mitigation: The plugin's update check only compares `>=` on versions — pre-release ordering is out of scope per the non-goals. A stable release will still be preferred because `GitHubService.GetLatestRelease` filters by stable channel.
- **[Risk] Tags with dots in suffixes** (e.g., `v1.0.0-beta.1` → `1.0.0-beta` after split-then-join → `1.0.0` after `-` strip) work correctly since `-` is stripped before splitting.
