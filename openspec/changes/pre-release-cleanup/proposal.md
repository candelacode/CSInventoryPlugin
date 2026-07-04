## Why

Before the v1 release, the plugin has a few loose ends that should be cleaned up: (1) backward-compatibility code for the legacy lowercase `sendcsitems` JSON key is unnecessary because v1 has not shipped yet, so any user touching a config now will adopt the canonical `SendCSItems` name; (2) the C# source files use the namespace `CSInventory.Plugin` / `CSInventory.Plugin.Tests`, but the project/folder names are `CSInventoryPlugin` / `CSInventoryPlugin.Tests` (no dot), which IDE0130 (`dotnet_style_namespace_match_folder`) flags as a namespace/folder mismatch; (3) the build emits `NU1903` (transitive High-severity vulnerability in `Microsoft.OpenApi` 2.0.0) warnings that can be cleaned up without diverging from ASF upstream.

## What Changes

- **Remove the legacy `sendcsitems` fallback and its supporting code**, since v1 has not been released:
  - Revert `CSBotConfig.TryGetSendCsItems` to the simpler 2-parameter signature `(IReadOnlyDictionary<string, JsonElement>?, out bool enabled)`. Drop the `out bool usedLegacyKey` parameter and the legacy-key `TryGetValue` branch.
  - Remove the legacy-key deprecation warning from `CSInventoryPlugin.IsSendCsItemsEnabled`.
  - Remove the four legacy-key/both-keys test cases and the `usedLegacyKey` assertions from the existing tests, restoring the simpler test set.
  - Update the corresponding requirements in the specs to drop legacy-fallback scenarios and the `usedLegacyKey` reporting.
- **Fix the namespace/folder mismatch** (IDE0130) by changing `namespace CSInventory.Plugin;` to `namespace CSInventoryPlugin;` in all 4 main source files and `namespace CSInventory.Plugin.Tests;` to `namespace CSInventoryPlugin.Tests;` in the test file, plus the corresponding `using` directive in the test file. This aligns the namespace with the folder/project names (`CSInventoryPlugin`, `CSInventoryPlugin.Tests`).
- **Suppress the `NU1903` warning in our `Directory.Build.props`** by adding `<NoWarn>$(NoWarn);NU1903</NoWarn>` with a clear comment. The vulnerability is in the transitive `Microsoft.OpenApi` 2.0.0 package pulled in by `Microsoft.AspNetCore.OpenAPI` 10.0.8 (the version ASF upstream pins). The proper fix is upstream; bumping the transitive version locally risks ASF compat, so suppressing the noise in our build is the pragmatic pre-release cleanup. (This is build-config only — it does not change any spec or code contract.)

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `cs-inventory-config`: The per-bot `SendCSItems` config requirement drops the legacy `sendcsitems` fallback (the requirement text and the scenarios "Legacy lowercase key used" and "Both canonical and legacy keys present" are removed). The plugin-API requirement drops its legacy-fallback sentence and scenario step.
- `bot-startup-cs-scan`: The "Respect sendcsitems config for startup scan" requirement drops its legacy-key clause, and its scenarios reference only `"SendCSItems"` (no legacy `"sendcsitems"` alternative).
- `plugin-code-organization`: The "Config parsing is decoupled from Bot" requirement reverts to the simpler 2-parameter `TryGetSendCsItems` signature, drops the `usedLegacyKey`-reporting clause, and drops the scenarios "Parsing via legacy key" and "Canonical key takes precedence over legacy key".

## Impact

- **Code**:
  - `CSInventoryPlugin/CSBotConfig.cs` — drop `usedLegacyKey` and the legacy `TryGetValue("sendcsitems", ...)` branch; revert to 2-param signature.
  - `CSInventoryPlugin/CSInventoryPlugin.cs` — drop the `usedLegacyKey` capture and the deprecation `LogGenericWarning`. Drop the `using` of any now-unused types.
  - `CSInventoryPlugin/CSItemUtilities.cs`, `CSInventoryPlugin/CSItemForwarder.cs` — namespace declaration changed from `CSInventory.Plugin` to `CSInventoryPlugin`.
  - `CSInventoryPlugin.Tests/CSInventoryPluginTests.cs` — namespace changed to `CSInventoryPlugin.Tests`; `using CSInventory.Plugin;` changed to `using CSInventoryPlugin;`; remove the 4 legacy/both-keys test cases and `usedLegacyKey` assertions from remaining cases; restore the simpler 6-case test set.
- **Build config**: `Directory.Build.props` — add `<NoWarn>$(NoWarn);NU1903</NoWarn>` with a comment explaining the upstream ASF vulnerability.
- **Docs / README**: no change (the README already documents `SendCSItems` as canonical and notes `sendcsitems` as a deprecated alias — after this change, the alias note can be dropped, but that's a doc-only follow-up; the deprecated-alias mention will be removed from the spec, and the README is out of strict spec scope).
- **User config**: No user-visible behavior change for users on v1 (none exist yet). Users who somehow had `sendcsitems` in a draft config will now have the key ignored → default `true`, which is the same as "not set" — this is acceptable pre-release.
- **Public API**: `CSBotConfig.TryGetSendCsItems` signature reverts (it is `internal static`, so this is not a public-API break).
