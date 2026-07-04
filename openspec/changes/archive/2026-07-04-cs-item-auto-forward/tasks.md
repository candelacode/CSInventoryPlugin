## 1. Update Plugin Class Structure

- [x] 1.1 Add `IBotModules` to the class interface list in `CSInventoryPlugin.cs`
- [x] 1.2 Add `using System.Collections.Concurrent;` import
- [x] 1.3 Add `BotAdditionalProperties` static `ConcurrentDictionary<string, IReadOnlyDictionary<string, JsonElement>?>` field

## 2. Implement IBotModules

- [x] 2.1 Implement `OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null)` method
- [x] 2.2 Add null check for `bot` parameter
- [x] 2.3 Store `additionalConfigProperties` in the dictionary keyed by `bot.BotName`

## 3. Update Config Reading

- [x] 3.1 Replace `bot.BotConfig.AdditionalProperties` in `GetSendCsItemsConfig` with lookup from `BotAdditionalProperties` dictionary
- [x] 3.2 Handle missing entry or null value by returning default (true)

## 4. Verify Build

- [x] 4.1 Run `dotnet build CSInventoryPlugin.slnx` and confirm 0 errors
- [x] 4.2 Verify plugin DLL is copied to ASF plugins output directory via PostBuild target
