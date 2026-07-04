## 1. Project Rename & Refactor

- [x] 1.1 Rename solution file from `BotManagerPlugin.slnx` to `CSInventoryPlugin.slnx` and update project references
- [x] 1.2 Rename main project folder `BotManagerPlugin/` to `CSInventoryPlugin/` and test folder `BotManagerPlugin.Tests/` to `CSInventoryPlugin.Tests/`
- [x] 1.3 Update `Directory.Build.props`: change `PluginName` to `CSInventoryPlugin`, update `Description` and `RepositoryUrl`
- [x] 1.4 Update `Directory.Packages.props` import path to match new folder structure
- [x] 1.5 Update `.github/` workflows to reference new project name
- [x] 1.6 Update `README.md` with new project name, description, and features

## 2. Remove Bot Management Code

- [x] 2.1 Delete `BotManagerController.cs` (REST API endpoints)
- [x] 2.2 Delete `BotManagementService.cs` (bot enable/disable logic)
- [x] 2.3 Remove associated tests from test project

## 3. Implement CS Inventory Monitor

- [x] 3.1 Rename `BotManagementPlugin.cs` to `CSInventoryPlugin.cs` and update namespace to `CSInventory.Plugin`
- [x] 3.2 Implement `IBotTradeOfferResults` interface on the plugin class
- [x] 3.3 In `OnBotTradeOfferResults()`, refresh inventory cache via `Bot.SteamInventory.RequestCacheAsync()`
- [x] 3.4 Check inventory cache for items with `appId == 730` (Counter Strike)
- [x] 3.5 Extract CS item asset IDs from inventory and group by bot

## 4. Implement Trade Notification

- [x] 4.1 Read bot's `sendcsitems` config via `BotConfig.GetAdditionalConfigProperties()`
- [x] 4.2 Check if master account is configured and is not the bot itself
- [x] 4.3 Call `Bot.Actions.SendInventory()` with detected CS items to master account
- [x] 4.4 Log trade offer results (success/failure/skip reasons)

## 5. Update Plugin Metadata

- [x] 5.1 Update `RepositoryName` property from `candelacode/BotManagerPlugin` to `candelacode/CSInventoryPlugin`
- [x] 5.2 Update `Name` property to `CSInventoryPlugin`
- [x] 5.3 Update `PluginName` in `Directory.Build.props`

## 6. Update Tests

- [x] 6.1 Rename test project classes and namespaces to `CSInventory.Plugin.Tests`
- [x] 6.2 Update test project references to new main project path
- [x] 6.3 Add tests for CS item detection logic (mock inventory data)
- [x] 6.4 Add tests for `sendcsitems` config parsing
- [x] 6.5 Add tests for trade notification logic

## 7. Cleanup Old Artifacts

- [x] 7.1 Archive or remove `openspec/specs/bot-management/` spec
- [x] 7.2 Update `openspec/specs/github-autoupdate/` spec to reflect new repo name
- [x] 7.3 Remove old bot management tests that are no longer relevant
- [x] 7.4 Init git with `git init` and create initial commit
