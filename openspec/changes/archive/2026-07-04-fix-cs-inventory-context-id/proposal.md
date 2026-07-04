## Why

The startup CS item scan is broken: it fetches the bot's inventory using the Steam community contextID (6), which is the context for Steam trading cards and gems (appId 753). CS items (appId 730) live in a different inventory context (contextID 2). As a result, the startup scan always returns 0 CS items and never forwards anything, even when the bot's inventory clearly contains CS items (e.g. visible at `steamcommunity.com/id/<user>/inventory#730`).

## What Changes

- Fix the startup inventory scan to fetch CS items using the correct contextID **2** (the game inventory context for appId 730) instead of `Asset.SteamCommunityContextID` (6).
- Introduce a named constant for the CS contextID so the intent is explicit and not confused with the Steam community context.
- Update the `bot-startup-cs-scan` spec to explicitly require fetching from the CS game inventory context (contextID 2), not the Steam community context.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities
- `bot-startup-cs-scan`: The "Scan bot inventory for CS items on startup" requirement is updated to explicitly specify that the inventory fetch uses the CS game inventory context (contextID 2), not the Steam community context (contextID 6).

## Impact

- **Code**: `CSInventoryPlugin/CSInventoryPlugin.cs` line 83 — change the `contextID` argument in the `bot.Actions.GetInventory()` call from `Asset.SteamCommunityContextID` to the CS contextID constant (2).
- **Specs**: Delta spec for `bot-startup-cs-scan` clarifying the correct contextID.
- **Dependencies**: No new dependencies; uses the same `Bot.Actions.GetInventory` API.
- **Compatibility**: No breaking changes; this is a bug fix that makes the existing startup scan actually work.
