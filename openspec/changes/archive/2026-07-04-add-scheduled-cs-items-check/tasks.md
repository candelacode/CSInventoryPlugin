## 1. Refactor shared forwarding logic

- [x] 1.1 Extract a private `ForwardCsItemsToMaster(Bot bot, HashSet<Asset> csItems)` helper in `CSInventoryPlugin.cs` that performs the `sendcsitems` config check, master-account validation (`GetFirstSteamMasterID`, self-master skip), `Bot.Actions.SendInventory` call, and success/failure logging.
- [x] 1.2 Refactor `OnBotTradeOfferResults` to filter CS items (appId 730) and call `ForwardCsItemsToMaster`, preserving existing behavior and log messages.

## 2. Bot startup CS scan

- [x] 2.1 Add `IBotConnection` to the `CSInventoryPlugin` interface list and import `ArchiSteamFarm.Plugins.Interfaces.IBotConnection`.
- [x] 2.2 Implement `OnBotDisconnected(Bot bot, EResult reason)` as a no-op returning `Task.CompletedTask`.
- [x] 2.3 Add a `ConcurrentDictionary<string, bool> BotStartupScanned` field to track which bots have already had their startup scan.
- [x] 2.4 Implement `OnBotLoggedOn(Bot bot)`: skip if bot is null/disconnected; skip and log if `sendcsitems` is false; skip if already in `BotStartupScanned`; otherwise mark scanned, call `Bot.Actions.GetInventory(appID: CSAppID, contextID: Asset.SteamCommunityContextID)`, and forward any returned CS items via `ForwardCsItemsToMaster`.
- [x] 2.5 Add `IBot` to the interface list (if not present) and implement `OnBotDestroy(Bot bot)` to remove the bot's entries from `BotStartupScanned` and `BotAdditionalProperties` so re-created bots get a fresh scan.

## 3. Tests

- [x] 3.1 Add a test verifying the startup scan forwards CS items when present (mock `Bot.Actions.GetInventory` returning CS assets and assert `SendInventory` is called with master SteamID).
- [x] 3.2 Add a test verifying the startup scan takes no action when the inventory has no CS items.
- [x] 3.3 Add a test verifying the startup scan is skipped when `sendcsitems` is false.
- [x] 3.4 Add a test verifying the startup scan is skipped when no master is configured and when master equals the bot's own SteamID.
- [x] 3.5 Add a test verifying the reconnect guard: a second `OnBotLoggedOn` for the same bot does not trigger a second scan.
- [x] 3.6 Run `dotnet test` and ensure all existing and new tests pass.

## 4. ASF plugin development documentation

- [x] 4.1 Add a "Plugin development" section to `README.md` summarizing: project structure/`.csproj` setup, `IPlugin` + `[Export(typeof(IPlugin))]`, common interfaces (`IASF`, `IBotModules`, `IBotConnection`, `IBotTradeOfferResults`, `IGitHubPluginUpdates`, `IPluginUpdates`), shared-dependency handling (`IncludeAssets="compile"`, `ExcludeAssets="all"`), the native-deps/OS-specific-build caveat, and GitHub-based auto-updates; link to https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Plugins-development.
- [x] 4.2 Verify the `asf-plugin-development` spec content matches the README section and covers all requirements (project structure, IPlugin/export, interfaces, shared deps, native deps, GitHub updates, custom updates, README exposure).

## 5. Build and verify

- [x] 5.1 Run `dotnet build -c Release` (or `build.bat`) and confirm the plugin compiles with no warnings/errors.
- [x] 5.2 Run `openspec validate add-scheduled-cs-items-check` and confirm the change validates cleanly.
- [x] 5.3 Confirm the built `CSInventoryPlugin.dll` loads in a generic ASF build and the startup scan logs on bot logon.
