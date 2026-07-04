## Context

CSInventory.Plugin currently forwards CS (appId 730) items to the master account only when ASF reports trade results via `IBotTradeOfferResults.OnBotTradeOfferResults`. Items that reach a bot's inventory through other paths (Steam grants, market activity, off-ASF trades, or items already present when ASF starts) are never detected. The plugin already implements `IASF`, `IGitHubPluginUpdates`, `IBotModules`, and `IBotTradeOfferResults`, and stores per-bot `additionalConfigProperties` (including `sendcsitems`) keyed by bot name.

The repository also lacks developer-facing documentation for ASF plugin development, despite being an ASF plugin itself. Contributors have no in-repo reference for the project layout, `IPlugin` interfaces, dependency handling, or auto-update conventions.

Relevant ASF APIs confirmed in the submodule:
- `IBotConnection.OnBotLoggedOn(Bot bot)` — fires when a bot successfully connects to Steam (`ArchiSteamFarm/Plugins/Interfaces/IBotConnection.cs:48`).
- `Bot.Actions.GetInventory(appID, contextID, filterFunction)` — fetches a bot's inventory, returns `(HashSet<Asset>? Result, string Message)` (`ArchiSteamFarm/Steam/Interaction/Actions.cs:177`).
- `Bot.Actions.SendInventory(items, targetSteamID)` — sends a trade offer with explicit items (`ArchiSteamFarm/Steam/Interaction/Actions.cs:418`).

## Goals / Non-Goals

**Goals:**
- Detect and forward CS items present in a bot's inventory when the bot comes online, reusing the existing trade-notification and `sendcsitems` config logic.
- Generalize the `trade-notification` spec so the notification trigger is mechanism-agnostic (trade results OR startup scan).
- Add ASF plugin development documentation (spec + README section) so contributors have a single in-repo reference.

**Non-Goals:**
- Periodic/scheduled background scanning on a timer (only the one-time startup scan is in scope).
- De-duplication or tracking of previously-forwarded item IDs across restarts.
- Changing the existing `OnBotTradeOfferResults` behavior or the `sendcsitems` config semantics.
- Building a new dependency on third-party libraries.

## Decisions

### Decision 1: Implement `IBotConnection` and trigger the scan from `OnBotLoggedOn`
The plugin will add `IBotConnection` to its interface list and implement `OnBotLoggedOn(Bot bot)` as the scan trigger. `OnBotDisconnected` is implemented as a no-op (`Task.CompletedTask`) since the plugin does not need disconnect handling.

**Rationale:** `OnBotLoggedOn` is the ASF-sanctioned hook for "bot is online and logged on", which is the earliest point where inventory access and trade offers are valid. It guarantees `bot.IsConnectedAndLoggedOn` is true, so the scan runs only on a healthy session.
**Alternatives considered:**
- `IBot.OnBotInit` / `IBotModules.OnBotInitModules` — rejected: these fire before the bot is logged on, so inventory/Steam web calls would fail.
- A polling timer — rejected: out of scope (non-goal) and adds resource cost; the user asked for a startup check.

### Decision 2: Fetch then forward via existing `Bot.Actions` APIs
The startup scan calls `bot.Actions.GetInventory(appID: CSAppID, contextID: Asset.SteamCommunityContextID)` to retrieve the bot's CS inventory, then forwards the resulting asset set with `bot.Actions.SendInventory(items, masterSteamID)`. This mirrors the trade-results path which already uses `SendInventory` with an explicit item collection.

**Rationale:** Reuses the exact send path the plugin already trusts, keeping behavior consistent (same master-validation, same logging, same failure handling). Fetching with an explicit appID filter avoids loading unrelated inventory.
**Alternatives considered:**
- `SendInventory(appID, contextID, targetSteamID, filterFunction)` single call — viable, but the explicit fetch-then-send keeps the "found N items" log line and master-validation identical to the existing flow, and avoids a second code path.

### Decision 3: Extract a shared `ForwardCsItemsToMaster(Bot bot, IReadOnlyCollection<Asset> items)` helper
The master-validation (`GetFirstSteamID`, self-master check), `sendcsitems` config check, `SendInventory` call, and logging are duplicated between `OnBotTradeOfferResults` and the new `OnBotLoggedOn` path. A private helper consolidates this so both triggers share one notification implementation.

**Rationale:** The `trade-notification` spec is being generalized to be mechanism-agnostic; the code should reflect that with a single forwarding routine. Reduces drift and test surface.
**Alternatives considered:**
- Duplicate the logic in `OnBotLoggedOn` — rejected: violates the generalized spec and doubles maintenance.

### Decision 4: Guard against re-firing on reconnect with a per-bot "scanned" flag
`OnBotLoggedOn` fires on every logon, including reconnects after drops. To keep the scan a one-time-per-startup action (matching "when a bot starts"), the plugin tracks a `ConcurrentDictionary<string, bool>` of already-scanned bot names, set after the first successful scan. The flag is reset in `OnBotDestroyed` so a bot re-created at runtime gets a fresh scan.

**Rationale:** Prevents duplicate trade offers for items whose prior trade is still pending (items remain in inventory until the offer is accepted).
**Alternatives considered:**
- Scan on every logon — rejected: risks duplicate outbound trade offers for pending items.
- Persist scanned state to disk — rejected: out of scope; in-memory state is sufficient for a startup check.

### Decision 5: ASF plugin development docs live in a spec + a README section
The `asf-plugin-development` spec captures the normative documentation requirements (project structure, `IPlugin`/`[Export]`, interfaces, dependency handling, native-deps caveat, GitHub/custom auto-updates). The README gains a concise "Plugin development" section that summarizes the same and links to the upstream wiki.

**Rationale:** Specs are the project's source of truth for capabilities; a README section makes the info discoverable to casual visitors. Keeping both in sync is enforced by the spec's README scenario.
**Alternatives considered:**
- README-only docs — rejected: less reviewable and not versioned alongside specs.
- A standalone `docs/` file — rejected: fragments documentation; the project already uses README + specs.

## Risks / Trade-offs

- [Reconnect duplicate trades] `OnBotLoggedOn` can fire multiple times if a bot drops and reconnects before the in-memory flag is set (race between first scan completing and a reconnect). → Mitigation: set the per-bot flag before awaiting the inventory fetch, and the `ForwardCsItemsToMaster` helper already relies on ASF's `SendInventory` which will simply fail if items are no longer available.
- [Pending trade offers] Items in a not-yet-accepted outbound trade still appear in inventory, so a startup scan may create a second offer for the same items if the master has a pending offer from a prior run. → Mitigation: documented as known behavior; ASF itself has the same characteristic for its `loot` action. No cross-restart dedup is in scope (non-goal).
- [Inventory fetch failures] `GetInventory` may return `null`/empty on rate limits or Steam errors at startup. → Mitigation: treat null/empty as "no items" and log a warning; no retry (matches existing failure behavior).
- [Native dependencies] No new native surface is introduced; the startup scan uses APIs ASF already uses internally, so OS-specific build trimming is unaffected. → Mitigation: none needed; the `asf-plugin-development` docs will still record the native-deps caveat for future contributors.
- [Doc drift] The README section and the spec could diverge over time. → Mitigation: the spec's "Expose plugin development documentation in README" requirement makes README presence a checkable expectation.

## Migration Plan

1. Implement `IBotConnection` + the shared `ForwardCsItemsToMaster` helper and refactor `OnBotTradeOfferResults` to call it.
2. Add `OnBotLoggedOn` startup scan with the per-bot scanned-flag guard; implement `OnBotDisconnected`/`OnBotDestroy` as no-ops/cleanup.
3. Add unit tests covering: startup scan with CS items, startup scan with no items, `sendcsitems=false` skip, no-master skip, self-master skip, and the reconnect guard.
4. Add the "Plugin development" section to `README.md` and the `asf-plugin-development` spec content.
5. Build (`dotnet build -c Release` / `build.bat`) and run tests; verify the plugin loads in a generic ASF build.

**Rollback:** Revert the commit; no data migration or config changes are involved. The `sendcsitems` config and existing trade-results behavior are unchanged, so users who never rely on the startup scan are unaffected.

## Open Questions

- Should the startup scan be opt-in via a new config flag (e.g. `sendcsitemsonstart`) separate from `sendcsitems`, or is reusing `sendcsitems` sufficient? Current decision: reuse `sendcsitems` to keep config minimal; revisit if users request finer control.
