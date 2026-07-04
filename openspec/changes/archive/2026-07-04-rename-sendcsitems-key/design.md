## Context

CSInventory.Plugin exposes a single per-bot config property that controls whether CS item trade notifications are sent. Today the JSON key is `sendcsitems` (all-lowercase), parsed by `CSBotConfig.TryGetSendCsItems()` via a case-sensitive `TryGetValue("sendcsitems", ...)` lookup against the `IReadOnlyDictionary<string, JsonElement>` that ASF passes through `IBotModules.OnBotInitModules` (`[JsonExtensionData]`).

ASF's own bot config properties are PascalCase (`SteamLogin`, `SteamMasterID`, `Enabled`, `TradingPreferences`, etc.). The lowercase `sendcsitems` is inconsistent with that convention. The key is referenced in: `CSBotConfig.cs` (lookup), `CSInventoryPlugin.cs` (log messages), `README.md` (config example + description), the unit tests, and four specs (`cs-inventory-config`, `trade-notification`, `bot-startup-cs-scan`, `plugin-code-organization`).

A hard rename (look up only `SendCSItems`) would be a silent breaking change: a user who today has `"sendcsitems": false` would, after upgrade, have the key not found → default `true` → their CS items would suddenly start being forwarded again. This is the primary risk the design must mitigate.

## Goals / Non-Goals

**Goals:**
- Make `SendCSItems` (PascalCase) the canonical, documented config key.
- Preserve existing user configs: `"sendcsitems"` continues to work.
- Surface a deprecation warning so users know to migrate to `SendCSItems`.
- Keep `CSBotConfig` `internal static` and Bot-free; keep the parsing pure (no logging inside the parser — caller logs).
- Update all docs, log messages, and tests to reflect the canonical key.

**Non-Goals:**
- Removing the legacy `sendcsitems` key (deferred to a future change after a deprecation period).
- Changing any other config property or ASF interface usage.
- Changing the trade-notification or startup-scan forwarding logic itself — only the config-key lookup and log wording change.
- Persisting/migrating user config files automatically.

## Decisions

### Decision 1: Backward-compatible rename with deprecation warning (not hard cutover)
**Choice:** Accept `SendCSItems` as canonical and keep `sendcsitems` as a supported legacy alias that emits a deprecation warning when used.

**Rationale:** A hard cutover silently re-enables forwarding for every user who currently sets `"sendcsitems": false` (the key vanishes → default `true`). That is a worse outcome than carrying a small amount of fallback code. A deprecation warning nudges migration without breaking anyone.

**Alternatives considered:**
- *Hard cutover, no fallback.* Simplest code, but silently breaks the `false`-configured majority this property exists for. Rejected.
- *Case-insensitive lookup.* Would accept `SendCSItems`, `sendcsitems`, `SENDCSITEMS`, etc. Hides typos and conflicts with ASF's case-sensitive `[JsonExtensionData]` dictionary behavior; also makes the deprecation warning ambiguous (can't tell "legacy lowercase" from "canonical"). Rejected.

### Decision 2: Lookup order — canonical first, then legacy
**Choice:** `CSBotConfig.TryGetSendCsItems()` does `TryGetValue("SendCSItems", ...)` first; if absent, `TryGetValue("sendcsitems", ...)`.

**Rationale:** The canonical key wins, so a user mid-migration who has both keys gets the new value. Keeping the order deterministic makes the "both keys present" behavior predictable and testable.

### Decision 3: Both keys present — canonical wins, log deprecation about the ignored legacy key
**Choice:** When both `SendCSItems` and `sendcsitems` are present, use `SendCSItems` and still log a deprecation warning (telling the user the legacy key is present and ignored, and to remove it).

**Rationale:** Canonical-wins is least surprising. Still warning about the legacy key drives cleanup so users don't keep a stale `false` lying around that they later think is in effect.

### Decision 4: Report "which key was used" via a new `out` parameter on the internal method
**Choice:** Change the internal signature to `internal static bool TryGetSendCsItems(IReadOnlyDictionary<string, JsonElement>? additionalProperties, out bool enabled, out bool usedLegacyKey)`. `usedLegacyKey` is `true` when the canonical key was absent and the legacy key was the one parsed (regardless of its validity).

**Rationale:** The parser must stay side-effect-free (no `Bot`, no logging), but the caller (`CSInventoryPlugin.IsSendCsItemsEnabled`) needs to know whether to emit the deprecation warning. An `out` flag is the smallest change that keeps the parser pure. Because `CSBotConfig` is `internal static`, this is not a public-API break — the plugin's public surface (the ASF interfaces) is unchanged.

**Alternatives considered:**
- *Return a small result struct (e.g., `SendCsItemsParseResult` with `Enabled`/`Valid`/`UsedLegacyKey`).* Cleaner long-term, but heavier than the change warrants for one extra bool. Could be revisited if more fields are needed.
- *Log the deprecation warning inside `CSBotConfig`.* Rejected — violates the existing "parser is pure, caller logs" requirement in `plugin-code-organization`.

### Decision 5: Deprecation warning emitted by the caller, once per config-read
**Choice:** `CSInventoryPlugin.IsSendCsItemsEnabled(bot)` logs `LogGenericWarning` with a deprecation message when `usedLegacyKey` is `true`. This runs on each startup scan (`OnBotLoggedOn`) and each trade-results check (`OnBotTradeOfferResults`) where the legacy key is the one found.

**Rationale:** Keeps the parser pure and reuses the existing warning path already used for invalid values. Repeating the warning per event is acceptable (these are infrequent) and avoids needing cross-call dedup state. If it proves noisy, a future change can deduplicate per bot.

**Alternatives considered:**
- *Warn once per bot (track warned set).* Avoids repeat noise but adds state and complexity for little gain. Deferred.
- *Warn only at `OnBotInitModules` time.* Cheaper, but config isn't actually read there today (only stored); would require reading it twice. Rejected to keep the change small.

### Decision 6: Log message wording references `SendCSItems`
**Choice:** Update the existing info/warning strings in `CSInventoryPlugin.cs` to say `SendCSItems` (e.g., `"Startup CS item scan skipped (SendCSItems = false)."`), and the invalid-value warning to reference `SendCSItems`.

**Rationale:** User-facing log lines should name the canonical key. The deprecation warning additionally names `sendcsitems` as the legacy alias to migrate away from.

## Risks / Trade-offs

- **[Silent re-enable if legacy fallback is ever removed later]** → Mitigation: deprecation warning now primes users to migrate before a future change drops the fallback; document the deprecation in the README.
- **[Repeat deprecation-warning noise per trade event]** → Mitigation: trade events are infrequent; accepted for now. Can add per-bot dedup in a follow-up if users complain.
- **[Users assume case-insensitivity after seeing both keys accepted]** → Mitigation: README explicitly states the canonical key is `SendCSItems` and that `sendcsitems` is a deprecated alias (not "any casing works").
- **[Internal signature change touches call sites and tests]** → Mitigation: only one caller (`IsSendCsItemsEnabled`) and the test file; both are updated in the same change. No public API impact.
- **[Both-keys-present user is surprised that legacy `false` is ignored]** → Mitigation: deprecation warning explicitly states the legacy key is being ignored in favor of `SendCSItems`.

## Migration Plan

1. Ship the change: `SendCSItems` canonical, `sendcsitems` legacy + deprecation warning.
2. Users do nothing to keep working; those who want to clean up rename `sendcsitems` → `SendCSItems` in their bot JSON and remove the warning.
3. README "Configuration" section documents `SendCSItems` as the key and notes `sendcsitems` as a deprecated alias.
4. A future change (out of scope) removes the legacy fallback after a reasonable deprecation period.

**Rollback:** Revert the commit. No data migration, no persisted state. Users who already migrated to `SendCSItems` would, after rollback, fall back through the case-sensitive `sendcsitems` lookup and find nothing → default `true`. To avoid surprise on rollback, the deprecation warning in the forward version tells users to keep using `SendCSItems` only after they are on the new build. (Accepted edge case; rollback is not a long-term state.)

## Open Questions

- Should the deprecation warning be deduplicated per bot to avoid repeat noise across multiple trade events? *Current decision: no, keep it simple; revisit if noisy.*
- When should the legacy `sendcsitems` fallback be removed? *Current decision: a future change after a deprecation period; not in scope here.*
