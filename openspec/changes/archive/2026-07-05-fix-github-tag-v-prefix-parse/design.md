## Context

`CSInventoryPlugin` implements `IGitHubPluginUpdates` and uses the default implementation for everything except `RepositoryName` and `Version`. The default flow, in `ArchiSteamFarm/.../Plugins/Interfaces/IGitHubPluginUpdates.cs:174`, builds a `System.Version` directly from `ReleaseResponse.Tag` using `new Version(releaseResponse.Tag)`. GitHub tags are conventionally prefixed with `v` (e.g., `v1.0.0`, `v1.0.0.5`), which `System.Version` does not accept, producing `FormatException: The input string 'v1' was not in a correct format`.

The exception is swallowed by the outer `try/catch` in `PluginsCore.UpdatePlugin` (`ArchiSteamFarm/.../Plugins/PluginsCore.cs:906`), so the update silently fails and users see only a stack trace in the log. The `github-autoupdate` spec already requires the `v` prefix to be stripped, but nothing implements that step. The bundled ASF build is a pinned submodule (`6.3.6.1`) and we want to keep it untouched, so the fix has to live inside the plugin.

## Goals / Non-Goals

**Goals:**
- Make plugin update checks succeed for repositories whose tags use the conventional `v`-prefix naming.
- Reuse the existing `GitHubService.GetLatestRelease` API so behavior stays aligned with ASF's default flow (same headers, same endpoint, same JSON shape).
- Keep the rest of the default `IGitHubPluginUpdates` semantics intact: respect `CanUpdate`, respect `forced`, respect the same asset-selection rules, log the same messages.
- Cover the change with unit tests that do not require network access.

**Non-Goals:**
- Patching the bundled ASF submodule (out of scope; the user explicitly chose to override locally).
- Supporting tags whose numeric portion uses non-standard separators (e.g., `v1-2-3`, `1.2.3-rc1`) — those will continue to surface as "no update available" via the outer catch.
- Changing the public surface of `CSInventoryPlugin` (the override uses a `protected` method on the interface).
- Touching the release CI workflow (`Directory.Build.props`, the `v`-tagged release process) — that already produces correct tags and is documented in the `github-autoupdate` spec.

## Decisions

**Decision 1: Implement `IPluginUpdates.GetTargetReleaseURL` explicitly on the plugin class.**
- Rationale: The default interface method `IGitHubPluginUpdates.GetTargetReleaseURL(Version, string, bool, bool, bool)` is `protected` and, as a default interface method (DIM), is **sealed** in the absence of the `virtual` modifier. The plugin class is also `sealed`, so neither inheritance nor `override` is available. The only public entry point is the explicit interface implementation `IPluginUpdates.GetTargetReleaseURL(Version, string, bool, GlobalConfig.EUpdateChannel, bool)`. Re-implementing it on the class takes precedence over the DIM at dispatch time, so the runtime will call our version. The class is also `sealed`, which is allowed for explicit interface implementations.
- Alternatives considered:
  - *Override the protected `GetTargetReleaseURL(Version, string, bool, bool, bool)` helper.* Rejected — the helper is a sealed DIM, not `virtual`, and `override` is therefore a compile error.
  - *Patch the ASF submodule.* Rejected — the user wants to keep ASF untouched, and the change would need to be upstreamed separately.
  - *Wrap `GitHubService` or `ReleaseResponse`.* Rejected — adds abstraction with no benefit and would diverge from ASF's behavior over time.

**Decision 2: Strip a single leading `v`/`V` only, then pass the result to `new Version(...)`.**
- Rationale: Matches the scenario documented in the existing `github-autoupdate` spec (`v1.2.3.4` → `1.2.3.4`). Does not attempt to handle arbitrary prefixes or `v`-in-the-middle cases; those should still surface as "no update available", consistent with how ASF treats them today.
- Alternatives considered:
  - *Use `Version.TryParse` with a fallback that searches for a leading `\d` substring.* Rejected — adds parsing complexity for tags that are not in the documented format, and the spec does not require it.
  - *Use a regex.* Rejected — overkill for stripping a single character.

**Decision 3: Do not catch `FormatException` in the plugin override.**
- Rationale: The outer `try/catch` in `PluginsCore.UpdatePlugin` already converts any exception into a logged error and a "no update" result, which is the same behavior the spec requires for unparseable tags. Re-throwing or re-catching would duplicate that handling.
- Alternatives considered:
  - *Catch and return `null` from the override.* Rejected — same end result, more code, and risks masking bugs we would want to see in logs.

**Decision 4: Mirror the default implementation's logging and asset-selection logic verbatim, but inline (do not call into the base).**
- Rationale: The default implementation in `IGitHubPluginUpdates.GetTargetReleaseURL(...)` is what we are replacing; calling it from our explicit implementation would re-trigger the buggy parse. The full body of the method is short (≈55 lines) and stable across ASF releases, so duplicating it inside a private helper is the cleanest way to swap only the broken step. The `GetPossibleNames` helper is also a `private` DIM and cannot be called from the class, so its body is re-inlined too.
- Alternatives considered:
  - *Use composition (wrap an inner updater object).* Rejected — adds a class for a one-method override and obscures the diff.
  - *Use reflection to invoke the DIM after swapping the tag.* Rejected — brittle and slower.
  - *Re-define `GetPossibleNames` as a `private` member on the class.* Rejected — would conflict with the DIM name; inlining its two `yield return` statements into the call site is shorter.

## Risks / Trade-offs

- **[Drift from upstream ASF]** → Mitigation: Add a unit test that exercises the *exact* `GitHubService.GetLatestRelease` arguments and re-asserts the same logging strings, so an ASF update that changes the flow surfaces as a test failure.
- **[Loss of the official `CanUpdate` short-circuit if the upstream contract changes]** → Mitigation: The override calls the inherited `CanUpdate` property directly; if the property is renamed upstream the build will fail loudly rather than silently degrade.
- **[Tags like `release-2024` still throw, surfacing as logged exceptions every update cycle]** → Mitigation: Acceptable — the exception is logged once per attempt and the result is "no update available". If this becomes noisy we can later add a soft-fail path that returns `null` when the tag does not start with `v` or a digit.
- **[Asset-selection logic duplicated in two places (plugin + ASF default)]** → Mitigation: The duplication is small and the spec is the source of truth. A comment in the override will note the upstream location to keep both copies in sync.

## Migration Plan

No migration needed. The change is in-place:
1. Rebuild the plugin (`dotnet build CSInventoryPlugin.slnx`).
2. Deploy the new `CSInventory.Plugin.dll` into the existing `plugins/CSInventoryPlugin/` directory of the ASF instance (the `PostBuild` target already does this for `bin/Release` and `bin/Debug`).
3. Trigger an `update` command or wait for the next update period; the next update check will use the override and succeed for `v`-prefixed tags.

Rollback is a single-file revert to `CSInventoryPlugin.cs` (the ASF submodule is untouched, so it requires no rollback).

## Open Questions

- Should the plugin also accept tags like `V1.0` (uppercase `V`, no patch segments) and treat them as `1.0.0.0`? Current decision: yes, because `System.Version` parses two-segment versions, and the test matrix will include the uppercase variant.
- Do we want to log a `LogGenericDebug` line when we strip a `v` prefix to help users diagnose? Current decision: no, to avoid noise; we only log at the same levels ASF's default does.
