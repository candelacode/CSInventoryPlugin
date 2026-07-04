# Architecture

Technical reference for CSInventory.Plugin: per-file layout, the ASF plugin interfaces used, build/release mechanics, and contributor notes. The [README](README.md) is the operator-facing entry point.

## Project layout

```
CSInventory.Plugin/
├── CSInventoryPlugin/                  # Plugin source
│   ├── CSInventoryPlugin.cs            # Entry point — ASF lifecycle + trade monitoring
│   ├── CSBotConfig.cs                  # SendCSItems config parser (pure, no Bot dependency)
│   ├── CSItemUtilities.cs              # CS item filtering, master-account evaluation
│   ├── CSItemForwarder.cs              # Startup scan + trade-offer forwarding
│   ├── CSInventoryPlugin.csproj
│   └── Properties/
├── CSInventoryPlugin.Tests/            # xUnit unit tests
│   ├── CSInventoryPluginTests.cs
│   └── CSInventoryPlugin.Tests.csproj
├── ArchiSteamFarm/                     # ASF source (git submodule)
├── Directory.Build.props               # Version + signing settings
├── Directory.Packages.props
└── CSInventoryPlugin.slnx
```

### Module boundaries

- `CSInventoryPlugin` (entry point) — only ASF interface implementations and per-bot state. All CS item logic is delegated.
- `CSBotConfig` — pure parser, no `Bot` dependency, no logging. Returns the parsed value, validity, and whether the user explicitly set the key.
- `CSItemUtilities` — pure helpers: `FilterCsItems`, `EvaluateMasterForForwarding`, the `CSAppID` / `CSContextID` constants.
- `CSItemForwarder` — depends on `Bot`; owns the `ForwardCsItemsToMaster` and `PerformStartupScan` methods.

## How it works (technical)

The plugin implements two ASF plugin interfaces and consults the per-bot `SendCSItems` config on each event:

### Startup scan — `OnBotLoggedOn`

Triggered when a bot becomes connected and logged on. If `SendCSItems` is `true`, the plugin calls `bot.Actions.GetInventory(appID: 730, contextID: 2)`, filters the result through `CSItemUtilities.FilterCsItems`, and forwards any found items via `CSItemForwarder.ForwardCsItemsToMaster`. The scan is guarded by a per-bot `ConcurrentDictionary` so reconnects do not re-trigger it.

### Trade-result forwarding — `OnBotTradeOfferResults`

Triggered after a bot processes a trade offer. The plugin walks `tradeResults[].ItemsToReceive`, filters CS items through `CSItemUtilities.FilterCsItems`, and forwards them via `CSItemForwarder.ForwardCsItemsToMaster`.

### SendCSItems config

- `CSBotConfig.TryGetSendCsItems(properties, out enabled, out explicitlySet)` returns:
  - `enabled = true` only when the user explicitly set `"SendCSItems": true`.
  - `explicitlySet = true` whenever the key is present in the JSON (valid or not).
  - The default is `false` for all "no value" cases (null dictionary, missing key, non-boolean value).
- The entry point calls `IsSendCsItemsEnabled(bot)` on every event, which:
  1. Emits the invalid-value warning if the parser returned `valid == false`.
  2. Emits `SendCSItems is enabled.` / `SendCSItems is disabled.` info line if `explicitlySet == true`.
  3. Stays silent if `explicitlySet == false`.
  4. Returns the `enabled` flag, which the caller uses to gate forwarding.

## Plugin development (ASF primer)

This section summarizes the ASF plugin contract. For the full guide, see the [ASF Plugins development wiki](https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Plugins-development).

### .csproj shape

A plugin is a standard .NET library targeting the same framework as ASF (here, `net10.0`). The `.csproj` references the ASF assembly and `System.Composition.AttributedModel` with `IncludeAssets="compile"` so they are not bundled into the plugin output:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Composition.AttributedModel" IncludeAssets="compile" />
    <PackageReference Include="SteamKit2" IncludeAssets="compile" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ArchiSteamFarm\ArchiSteamFarm\ArchiSteamFarm.csproj" ExcludeAssets="all" Private="false" />
  </ItemGroup>
</Project>
```

### IPlugin and export

A plugin class must inherit from `IPlugin` (or a more specialized interface) and be annotated with `[Export(typeof(IPlugin))]` so ASF can discover and load it via `System.Composition`:

```csharp
using System.Composition;
using System.Threading.Tasks;
using ArchiSteamFarm;
using ArchiSteamFarm.Plugins;

namespace MyPlugin;

[Export(typeof(IPlugin))]
public sealed class MyPlugin : IPlugin {
    public string Name => nameof(MyPlugin);
    public Version Version => typeof(MyPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

    public Task OnLoaded() {
        ASF.ArchiLogger.LogGenericInfo("Hello World!");
        return Task.CompletedTask;
    }
}
```

### Common plugin interfaces

| Interface | Purpose |
|---|---|
| `IASF` | ASF initialization (`OnASFInit`) |
| `IBot` | Bot lifecycle (`OnBotInit`, `OnBotDestroy`) |
| `IBotModules` | Per-bot config properties (`OnBotInitModules`) |
| `IBotConnection` | Bot connection events (`OnBotLoggedOn`, `OnBotDisconnected`) |
| `IBotTradeOfferResults` | Trade offer processing results (`OnBotTradeOfferResults`) |
| `IBotSteamClient` | Custom SteamKit2 handlers |
| `IBotCommand2` | Custom commands |
| `IGitHubPluginUpdates` | GitHub-based automatic plugin updates |
| `IPluginUpdates` | Custom update mechanism |

See [`ArchiSteamFarm.Plugins.Interfaces`](https://github.com/JustArchiNET/ArchiSteamFarm/tree/main/ArchiSteamFarm/Plugins) and the [ExamplePlugin](https://github.com/JustArchiNET/ArchiSteamFarm/tree/main/ArchiSteamFarm/Plugins/ExamplePlugin) for full examples.

### Shared dependency handling

Dependencies already included in ASF (`ArchiSteamFarm`, `SteamKit2`, `AngleSharp`, etc.) should be marked with `IncludeAssets="compile"` to avoid bundling them in the plugin output. Only include libraries that ASF does not ship (e.g. `Discord.Net`).

### Native dependencies caveat

ASF OS-specific builds trim the .NET runtime to reduce size. Plugins that use .NET features not covered by the trimmed runtime may encounter `System.MissingMethodException` or `System.Reflection.ReflectionTypeLoadException`. Verify against the ASF **generic** build first; running custom plugins in the generic ASF flavor is recommended.

## Automatic updates (interface side)

The plugin implements `IGitHubPluginUpdates` with `RepositoryName = "candelacode/CSInventoryPlugin"`. ASF picks it up when the user opts in via `PluginsUpdateMode` / `PluginsUpdateList` (see [README → Automatic updates](README.md#automatic-updates)).

ASF supports two update interfaces in general:

- **`IGitHubPluginUpdates`**: GitHub-based. Set `RepositoryName`, use version-parsable tags (e.g. `v1.0.0.0`), ensure the plugin's `Version` matches the tag, and attach a `.zip` release asset with the plugin DLL at the root.
- **`IPluginUpdates`**: Custom. Override `GetTargetReleaseURL()`. Supports `OnPluginUpdateProceeding()` and `OnPluginUpdateFinished()` hooks.

## Releasing (maintainer-side)

To cut a new release:

1. Bump `<Version>` in `Directory.Build.props` to the new 4-part value (e.g. `1.0.1.0`). The version MUST be 4 parts (`Major.Minor.Build.Revision`) and parsable as a `System.Version`. The 3-part form `1.0.1` is not accepted.
2. Commit on `main`:
   ```bash
   git add Directory.Build.props
   git commit -m "Bump version to 1.0.1.0"
   git push origin main
   ```
3. Tag and push:
   ```bash
   git tag v1.0.1.0
   git push origin v1.0.1.0
   ```
4. The `.github/workflows/publish.yml` `release` job runs automatically. It builds the plugin on `ubuntu-latest`, `macos-latest`, and `windows-latest`, generates `SHA512SUMS`, and publishes a non-prerelease GitHub release with `CSInventoryPlugin.zip` (plus per-OS zips) attached.

The tag body (the part after `v`) MUST match `<Version>` exactly. If they differ, the build still succeeds but the DLL's version will not match the tag and ASF will not auto-update users.

## Contributing

- Specs live in [`openspec/specs/`](openspec/specs/) and are the source of truth for behavior. Active change proposals live in [`openspec/changes/`](openspec/changes/); archived changes are under `openspec/changes/archive/`.
- To propose a behavior change, create a new change directory using the OpenSpec CLI and fill in `proposal.md`, `design.md`, `tasks.md`, and any `specs/<capability>/spec.md` deltas. The CI / reviewers expect spec-level coverage for any behavior change.
- Keep modules pure where possible: parsers and utilities should not depend on `Bot` or perform logging. Logging stays in the entry point.
