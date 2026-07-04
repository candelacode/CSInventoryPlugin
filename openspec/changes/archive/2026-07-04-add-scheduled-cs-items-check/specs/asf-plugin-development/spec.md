## ADDED Requirements

### Requirement: Document ASF plugin project structure
The repository SHALL include developer documentation describing the required project structure for an ArchiSteamFarm plugin, including targeting the appropriate .NET framework, referencing the `ArchiSteamFarm` assembly, and referencing `System.Composition.AttributedModel`.

#### Scenario: Developer reads plugin setup guide
- **WHEN** a contributor opens the developer documentation
- **THEN** the documentation describes the `.csproj` setup
- **AND** explains `PackageReference Include="System.Composition.AttributedModel" IncludeAssets="compile"`
- **AND** explains referencing ASF via `ProjectReference` with `ExcludeAssets="all" Private="false"` or via `Reference` with `HintPath`

### Requirement: Document IPlugin interface and export
The documentation SHALL explain that a plugin class must inherit from `IPlugin` (or a more specialized interface like `IASF`) and be annotated with `[Export(typeof(IPlugin))]` so ASF can discover and load it during runtime.

#### Scenario: Minimal plugin example documented
- **WHEN** a contributor reads the plugin development guide
- **THEN** the documentation includes a minimal example plugin class
- **AND** the example shows `Name`, `Version`, and `OnLoaded()` members
- **AND** the example shows the `[Export(typeof(IPlugin))]` attribute

### Requirement: Document plugin interfaces
The documentation SHALL describe the available ASF plugin interfaces (e.g. `IASF`, `IBotModules`, `IBotTradeOfferResults`, `IGitHubPluginUpdates`, `IPluginUpdates`) and what each is used for.

#### Scenario: Contributor needs to hook bot lifecycle
- **WHEN** a contributor wants to act on bot startup or trade events
- **THEN** the documentation lists the relevant interfaces and their callback methods
- **AND** references the `ArchiSteamFarm.Plugins` namespace and `ExamplePlugin` for full examples

### Requirement: Document shared dependency handling
The documentation SHALL explain that dependencies already included in ASF (e.g. `ArchiSteamFarm`, `SteamKit2`, `AngleSharp`) SHOULD be marked with `IncludeAssets="compile"` to avoid bundling them, reducing memory footprint and plugin size.

#### Scenario: Plugin depends on a library ASF already ships
- **WHEN** a plugin references a library that ASF already includes
- **THEN** the documentation instructs the developer to mark it with `IncludeAssets="compile"`
- **AND** explains that only libraries ASF does not include should be bundled in the published output

### Requirement: Document native dependencies caveat
The documentation SHALL explain that ASF OS-specific builds trim the .NET runtime, which can cause `System.MissingMethodException` or `System.Reflection.ReflectionTypeLoadException` for plugins using trimmed runtime features, and that generic builds are recommended for custom plugins.

#### Scenario: Plugin fails on OS-specific build
- **WHEN** a plugin throws `MissingMethodException` on an OS-specific ASF build
- **THEN** the documentation explains this is likely a trimmed native dependency
- **AND** recommends verifying against the ASF generic build

### Requirement: Document GitHub-based auto-updates
The documentation SHALL describe the `IGitHubPluginUpdates` interface, including the requirements for `RepositoryName`, version-tag matching, release zip asset layout (plugin DLL at the zip root), and the `PluginsUpdateMode`/`PluginsUpdateList` ASF config values.

#### Scenario: Contributor implements GitHub updates
- **WHEN** a contributor wants their plugin to auto-update
- **THEN** the documentation explains implementing `IGitHubPluginUpdates`
- **AND** explains that release tags must parse to a `System.Version`
- **AND** explains that each release must include a zip with the plugin DLL at the root

### Requirement: Document custom auto-updates
The documentation SHALL describe the `IPluginUpdates` interface as a lower-level alternative for custom update mechanisms when `IGitHubPluginUpdates` is insufficient.

#### Scenario: Non-GitHub update mechanism
- **WHEN** a plugin cannot use GitHub-based updates
- **THEN** the documentation explains implementing `IPluginUpdates.GetTargetReleaseURL()`
- **AND** explains the `OnPluginUpdateProceeding()` and `OnPluginUpdateFinished()` hooks

### Requirement: Expose plugin development documentation in README
The repository README SHALL include a developer documentation section that summarizes ASF plugin development and links to the upstream ASF Plugins development wiki for further reference.

#### Scenario: README contains developer docs
- **WHEN** a visitor reads the repository README
- **THEN** the README includes a "Plugin development" section
- **AND** the section summarizes the project structure, interfaces, dependencies, and auto-update approach
- **AND** links to https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Plugins-development
