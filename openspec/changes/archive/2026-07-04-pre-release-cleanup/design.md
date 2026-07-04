## Context

CSInventory.Plugin v1 has not been released yet. Three pre-release cleanups are bundled into this change:

1. **Legacy `sendcsitems` code removal.** The previous change (`rename-sendcsitems-key`, archived) introduced a canonical-first `SendCSItems` lookup with a legacy `sendcsitems` fallback, an `out bool usedLegacyKey` parameter on `CSBotConfig.TryGetSendCsItems`, a deprecation warning in `CSInventoryPlugin.IsSendCsItemsEnabled`, and 4 additional test cases. With v1 not shipped, no user has the legacy key in their config, so the fallback and deprecation machinery are dead code that complicates the parser, the call site, and the tests.

2. **Namespace/folder mismatch (IDE0130).** The `.editorconfig` enables `dotnet_style_namespace_match_folder = true:warning`, and the project/folder names are `CSInventoryPlugin` and `CSInventoryPlugin.Tests`, but the source files declare `namespace CSInventory.Plugin;` and `namespace CSInventory.Plugin.Tests;`. The IDE surfaces this as an error/warning. The fix is a mechanical rename of the namespace declarations (and the corresponding `using` in the test file) to drop the dot.

3. **`NU1903` build warnings.** `dotnet build` reports 6 `NU1903` warnings (2 per project × 3 projects: main, test, ASF submodule) for a High-severity vulnerability in the transitive `Microsoft.OpenApi` 2.0.0 package, pulled in by `Microsoft.AspNetCore.OpenAPI`. ASF upstream pins `Microsoft.AspNetCore.OpenAPI` to `10.0.8` in `ArchiSteamFarm/Directory.Packages.props` (which the root `Directory.Packages.props` imports via CPM), and ASF itself emits the same warning. The proper fix is upstream; locally overriding the transitive `Microsoft.OpenApi` to a patched version risks binary incompat with ASF, so the pragmatic pre-release cleanup is to suppress `NU1903` in our own `Directory.Build.props` with a clear comment.

## Goals / Non-Goals

**Goals:**
- Drop the legacy `sendcsitems` code path: simpler `CSBotConfig` parser, simpler `IsSendCsItemsEnabled`, simpler test set.
- Align C# namespaces with the folder/project names to clear IDE0130.
- Clean the build output of `NU1903` warnings in our project, with a documented justification pointing to ASF upstream.
- Update the specs to reflect the simpler post-cleanup contract.

**Non-Goals:**
- Removing the `sendcsitems` mention from `README.md` (the README's "deprecated alias" note becomes inaccurate after this change, but README is out of the spec-driven scope of this change; it's a doc-only follow-up).
- Fixing the `Microsoft.OpenApi` 2.0.0 vulnerability itself (upstream ASF concern; out of scope).
- Changing any runtime behavior beyond removing the legacy fallback (no functional change for users on `SendCSItems`).

## Decisions

### Decision 1: Revert `CSBotConfig.TryGetSendCsItems` to a 2-parameter signature
**Choice:** `internal static bool TryGetSendCsItems(IReadOnlyDictionary<string, JsonElement>? additionalProperties, out bool enabled)`. No `usedLegacyKey` out parameter, no `TryGetValue("sendcsitems", ...)` fallback.

**Rationale:** With v1 not released, the legacy fallback is dead code. Reverting to the simpler signature eliminates the extra out parameter (which exists only to drive the deprecation warning), reduces the parser's branches, and makes the call site and tests simpler. The parser stays pure (no `Bot`, no logging, no side effects), as the existing `plugin-code-organization` spec already requires.

**Alternatives considered:**
- *Keep the 3-parameter signature and just have `usedLegacyKey` always return `false`.* Leaves the dead parameter and confuses readers ("what's this flag for?"). Rejected — clean revert is better.
- *Keep the legacy fallback but stop emitting the deprecation warning.* Still dead code for v1; adds complexity for no benefit. Rejected.

### Decision 2: Namespace rename — `CSInventory.Plugin` → `CSInventoryPlugin` (and `.Tests` analog)
**Choice:** Change `namespace CSInventory.Plugin;` → `namespace CSInventoryPlugin;` in the 4 main source files, and `namespace CSInventory.Plugin.Tests;` → `namespace CSInventoryPlugin.Tests;` plus the `using CSInventory.Plugin;` → `using CSInventoryPlugin;` in the test file.

**Rationale:** The folder names (`CSInventoryPlugin`, `CSInventoryPlugin.Tests`) and project/assembly names match the new namespaces. The assembly name stays `CSInventoryPlugin` (and `CSInventoryPlugin.Tests`), and `InternalsVisibleTo Include="CSInventoryPlugin.Tests"` in the main `.csproj` already references the assembly name, so no `.csproj` change is needed. The rename is a pure source-level change that satisfies IDE0130.

**Alternatives considered:**
- *Rename the folders/projects instead.* Far more invasive (affects `.slnx`, `.csproj`, build output paths, PostBuild copy targets in the main `.csproj`). The namespace rename is the minimal fix. Rejected.

### Decision 3: Suppress `NU1903` in `Directory.Build.props` (do not override the transitive version)
**Choice:** Add `<NoWarn>$(NoWarn);NU1903</NoWarn>` to the root `Directory.Build.props` with an XML comment explaining the upstream `Microsoft.OpenApi` 2.0.0 vulnerability, that ASF pins `Microsoft.AspNetCore.OpenAPI` to 10.0.8 in `ArchiSteamFarm/Directory.Packages.props`, and that the fix should land upstream.

**Rationale:** The vulnerability is transitive through `Microsoft.AspNetCore.OpenAPI`, whose version is centrally managed by ASF (CPM enforces this via `ManagePackageVersionsCentrally=true` in ASF's `Directory.Build.props`). Overriding `Microsoft.OpenApi` to a patched version in our own `Directory.Packages.props` would force a version that diverges from what ASF is compiled against — a binary-compat risk that outweighs the benefit, since ASF itself is exposed to the same transitive dep. Suppressing the warning in our build is honest: it acknowledges the issue, attributes it to the upstream package, and stops noise. The proper fix is for ASF to bump `Microsoft.AspNetCore.OpenAPI` to a version whose transitive `Microsoft.OpenApi` is patched; at that point, we remove the `NoWarn` entry.

**Alternatives considered:**
- *Override `Microsoft.OpenApi` to a patched version via `<PackageVersion Include="Microsoft.OpenApi" Version="2.0.1"/>` in our `Directory.Packages.props`.* Risk of binary-compat divergence from ASF (which links the same package at 2.0.0). Rejected for pre-release cleanup.
- *Leave the warnings as-is.* They're real warnings in our build output and the user asked to clean "some of the warnings." Rejected.
- *Suppress in each `.csproj` instead of `Directory.Build.props`.* Duplicates the suppression in 3 places. Rejected — `Directory.Build.props` is imported by all projects, single point of control.

## Risks / Trade-offs

- **[Removing legacy fallback means any draft user config with `"sendcsitems": false` silently flips to default `true` on upgrade]** → Mitigation: v1 has not been released, so there are no real users yet; this is acceptable pre-release. The proposal documents this.
- **[Namespace rename is a source-level breaking change for anyone referencing the old namespace]** → Mitigation: The plugin is a single internal codebase; no external code references `CSInventory.Plugin.*`. `InternalsVisibleTo` uses assembly names, not namespaces, so the rename is safe.
- **[Suppressing `NU1903` hides a real High-severity vulnerability warning from the build]** → Mitigation: Document the upstream cause in an XML comment next to the `NoWarn` entry, and call out in the migration plan that the suppression should be revisited when ASF updates `Microsoft.AspNetCore.OpenAPI` to a version with a patched `Microsoft.OpenApi`.
- **[If ASF upstream bumps `Microsoft.AspNetCore.OpenAPI`, our suppression becomes stale and might suppress new warnings]** → Mitigation: The suppression is scoped to `NU1903` only; it does not blanket-suppress all warnings. When the upstream bump lands, the suppression can be removed.
- **[Loss of `usedLegacyKey` test coverage]** → Mitigation: The legacy fallback is being removed entirely, so the legacy-fallback tests are no longer meaningful. The remaining 6 tests cover true/false/missing/null/invalid-type/number, which is the full surface of the simplified parser.

## Migration Plan

1. Apply the code, test, namespace, and `Directory.Build.props` changes.
2. Run `dotnet build -c Release` and `dotnet test` to confirm the build is clean and all tests pass.
3. The `Directory.Build.props` `NoWarn` entry includes a comment naming ASF upstream; revisit and remove the suppression when ASF bumps `Microsoft.AspNetCore.OpenAPI` past the `Microsoft.OpenApi` 2.0.0 vulnerability.
4. A separate doc-only change (out of scope) can remove the `sendcsitems` "deprecated alias" mention from `README.md`, since after this change there is no legacy fallback to deprecate.

**Rollback:** Revert the commit. The namespace rename is a pure source-level change. The `CSBotConfig` and `IsSendCsItemsEnabled` revert restores the legacy-fallback code path. The test file reverts to the legacy-aware tests. The `NoWarn` removal restores the build warnings (no harm done, just noise).

## Open Questions

- Should the `sendcsitems` mention in `README.md` be removed in this same change, or as a follow-up? *Current decision: follow-up — keeps this change focused on code/build-config; the README is a doc-only edit that doesn't need spec coverage.*
