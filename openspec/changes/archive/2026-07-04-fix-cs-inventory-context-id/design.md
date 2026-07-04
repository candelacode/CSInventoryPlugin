## Context

The startup CS item scan (added in the `add-scheduled-cs-items-check` change) calls `Bot.Actions.GetInventory(appID: CSAppID, contextID: Asset.SteamCommunityContextID)` to fetch the bot's CS inventory. `Asset.SteamCommunityContextID` is `6`, which is the Steam community inventory context used by Steam trading cards, gems, and other Steam items (appId 753). CS items (appId 730) are stored in a separate game inventory context — contextID **2**. Because the fetch targets the wrong context, it always returns 0 items and the plugin never forwards CS items on startup.

Steam inventory contexts are per-app:
- App 753 (Steam): contextID 6 (community)
- App 730 (CS:GO / CS2): contextID 2 (game inventory)
- App 440 (TF2), 570 (Dota 2): also contextID 2

The trade-results path (`OnBotTradeOfferResults`) is unaffected because it filters `ParseTradeResult.ItemsToReceive` by `AppID == 730`, and those assets already carry the correct contextID from the trade offer data.

## Goals / Non-Goals

**Goals:**
- Fix the startup scan to fetch CS items from the correct contextID (2) so CS items are actually found and forwarded.

**Non-Goals:**
- Changing the trade-results detection logic (it already works correctly).
- Supporting multiple contextIDs for appId 730 (contextID 2 is the only inventory context for CS).
- Changing which items are filtered (still appId 730 only).

## Decisions

### Decision 1: Use a dedicated constant `CSContextID = 2` for the CS inventory fetch
Add `private const ulong CSContextID = 2;` to the plugin class and pass it as the `contextID` argument to `bot.Actions.GetInventory()`.

**Rationale:** A named constant makes the intent explicit and avoids confusion with `Asset.SteamCommunityContextID` (6). The value 2 is the standard game inventory context for CS:GO/CS2 and is stable across Steam's inventory API.
**Alternatives considered:**
- Hardcode `2` inline — rejected: less readable and prone to future confusion with contextID 6.
- Fetch from all contexts — rejected: `GetInventory` takes a single contextID; unnecessary complexity for a single-context game.

## Risks / Trade-offs

- [Valve changes CS contextID] If Valve ever moved CS items to a different contextID, the scan would break again. → Mitigation: contextID 2 has been stable for CS:GO/CS2 since launch; this is the same contextID ASF's own `loot` commands use for game inventories.
- [No additional risk] The fix is a one-line change with no architectural impact.
