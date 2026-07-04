## 1. Fix CS inventory contextID

- [x] 1.1 Add a `private const ulong CSContextID = 2;` constant in `CSInventoryPlugin.cs` next to the existing `CSAppID` constant.
- [x] 1.2 Change the `bot.Actions.GetInventory()` call in `OnBotLoggedOn` from `contextID: Asset.SteamCommunityContextID` to `contextID: CSContextID`.

## 2. Tests

- [x] 2.1 Add a test verifying `CSContextID` equals 2 (guard against accidental change to the wrong context).
- [x] 2.2 Run `dotnet test` and ensure all existing and new tests pass.

## 3. Build and verify

- [x] 3.1 Run `dotnet build -c Release` and confirm 0 errors.
- [x] 3.2 Run `openspec validate fix-cs-inventory-context-id` and confirm the change validates cleanly.
