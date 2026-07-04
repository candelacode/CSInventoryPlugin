## Context

CSInventory.Plugin reads a per-bot `SendCSItems` boolean from the ASF `[JsonExtensionData]` dictionary and uses it to decide whether to forward CS items to the master account. The current behavior is:

- `CSBotConfig.TryGetSendCsItems()` returns `enabled = true` whenever the key is missing, the dictionary is null, or the value is not a boolean. `valid` is `true` for the first two cases and `false` for non-boolean values.
- `CSInventoryPlugin.IsSendCsItemsEnabled(bot)` calls the parser, logs a warning for invalid values, and returns the `enabled` flag.
- `OnBotLoggedOn` and `OnBotTradeOfferResults` only log when `SendCSItems` is explicitly `false` (the "skipped" lines). They stay silent when it is `true` or unset.
- The README documents the default as `true` and the "Zero Configuration" feature is sold on that basis.

The new product requirement is: the default is `false`; missing-key is silent; `true` and `false` both produce a single info line that states the effective state.

The change is small (two C# files, the tests, the README, and four specs) but crosses a public behavior boundary — the default flips — so the design needs to be deliberate about (a) what we say in the log, (b) when we say it, and (c) how we keep the parser pure (per the `plugin-code-organization` spec).

## Goals / Non-Goals

**Goals:**

- Make the default value of `SendCSItems` `false` everywhere — parser, callers, tests, docs, specs.
- Add a single info log line per bot, per event, that states the effective state (`enabled` or `disabled`) when the property is explicitly set in the bot's JSON.
- Stay silent (no config-related log) when the property is absent — including when `additionalProperties` itself is null.
- Keep `CSBotConfig` a pure, side-effect-free, Bot-free parser. Logging stays in `CSInventoryPlugin`.
- Preserve the existing "Invalid SendCSItems value, expected boolean" warning path.
- Update all four affected specs and the README so the documented behavior matches the implemented behavior.

**Non-Goals:**

- Adding a new config property (e.g., `SendCSItemsDefault`, an `enabled`/`disabled`/`default` enum). The existing boolean is fine; only the default and the logging change.
- Persisting or migrating user JSON config files. The behavior change is documented in the release notes (out of scope for code, in scope for the README).
- Removing the legacy `sendcsitems` key. That lives in the `rename-sendcsitems-key` change and is not part of this work.
- Deduplicating the new info log per bot. Trade events and startup events are infrequent enough that one log per event is fine; a future change can add a "logged once per bot" cache if it proves noisy.
- Changing the trade-notification or startup-scan forwarding logic itself. Only the config default and the log lines change.

## Decisions

### Decision 1: Flip the default in `CSBotConfig` from `true` to `false` in all "no value" branches

**Choice:** In `CSBotConfig.TryGetSendCsItems()`, change `enabled = true` to `enabled = false` in:

- The `additionalProperties == null` branch.
- The "key not found" branch.
- The "value is not a boolean" branch (the fallback when parsing fails).
- The "value is not a boolean AND we want to return invalid" branch (the `return false` at the bottom).

**Rationale:** The parser is the single source of truth for the "what is the effective value?" question. The new default has to be expressed in one place so callers do not need to re-interpret. Keeping the parser pure (no `Bot`, no logging) and changing only the `enabled` value keeps `plugin-code-organization` satisfied.

**Alternatives considered:**

- *Default `true` in the parser and re-interpret to `false` in the caller.* Splits the truth across two files; future callers can forget to re-interpret. Rejected.
- *Add a separate "is the value set?" flag and let callers decide the default.* Heavier API for a simple change; the absence of the key is already what "not set" means in JSON. Rejected.

### Decision 2: Distinguish "explicitly set" from "default" with a new `out bool explicitlySet` parameter

**Choice:** Change `CSBotConfig.TryGetSendCsItems()` to `internal static bool TryGetSendCsItems(IReadOnlyDictionary<string, JsonElement>? additionalProperties, out bool enabled, out bool explicitlySet)`.

- `explicitlySet = true` when the key was present and parsed as a boolean (valid or not — "explicitly set" means "the user wrote it in their JSON").
- `explicitlySet = false` when the dictionary is null, the key is absent, or the value is null.

The `valid` out parameter is preserved (true for `true`/`false`/missing; false for non-boolean). The `enabled` out parameter is now always the *effective* value: the parsed boolean when explicit, `false` otherwise.

**Rationale:** The "no log when unset" requirement means the caller needs to know whether the user wrote the key. Without `explicitlySet`, the caller cannot tell `{}` (unset, silent) from `{"SendCSItems": false}` (set, log "disabled"). The simplest, parser-pure way to expose that is an `out` bool, mirroring the pattern already used for `usedLegacyKey` in the `rename-sendcsitems-key` change.

**Alternatives considered:**

- *Return a small result struct (`SendCsItemsParseResult` with `Enabled` / `Valid` / `ExplicitlySet`).* Cleaner long-term, but more files touched and a bigger test diff for a 3-field result. The `out`-flag style is consistent with the existing legacy-key flag added in the rename change. Deferred — revisit if a fourth field appears.
- *Have the parser return a nullable bool (`bool?` meaning "explicitly set", `null` meaning "use default") and let the caller default to `false`.* More idiomatic in modern C#, but it forces the caller to write `result ?? false` at every call site and obscures `valid`. Rejected.
- *Use a sentinel value (e.g., `enabled = true` means "use new default of false").* Confusing and bug-prone. Rejected.

### Decision 3: Single info log line per event, per bot, in `CSInventoryPlugin`

**Choice:** In `OnBotLoggedOn` and `OnBotTradeOfferResults`, when the parser reports `explicitlySet == true`, emit a single `bot.ArchiLogger.LogGenericInfo` line of the form:

- `"{bot.BotName}: SendCSItems is enabled."` when `enabled == true`.
- `"{bot.BotName}: SendCSItems is disabled."` when `enabled == false`.

When `explicitlySet == false`, emit no config-related log line at all (the "stay silent" path).

The existing "skipped" lines (`"Startup CS item scan skipped (SendCSItems = false)."` and `"CS item notification skipped (SendCSItems = false)."`) are removed. The new "disabled" line covers the same case without the verb "skipped", and is also emitted when the user explicitly set `false` even if no CS items arrive (so operators see the config is off without having to wait for a trade).

**Rationale:** A single consistent log line per event is the minimum needed to confirm "I read your config and here is what I will do" without spamming the operator. Removing the old "skipped" lines prevents double-logging (`"SendCSItems is disabled."` + `"...skipped (SendCSItems = false)."` would say the same thing twice).

**Alternatives considered:**

- *Log once per bot per ASF session (deduped in a `ConcurrentDictionary`).* Cleaner long-term but adds state. Trade events are infrequent; the dedup can be a follow-up. Rejected for this change.
- *Log only at `OnBotInitModules` time.* Cheaper, but config is currently only *stored* at init time, not *read*. Reading twice (init + logon) splits the truth. Rejected.
- *Log at `LogGenericDebug` instead of `LogGenericInfo`.* Some operators treat debug as "off by default"; the new log is meant to be visible. Rejected.

### Decision 4: Keep the invalid-value warning and add the disabled info line below it

**Choice:** When the JSON has a non-boolean value (e.g., `"SendCSItems": "yes"`), the parser returns `valid = false, enabled = false, explicitlySet = true`. The caller:

1. Emits the existing `"Invalid SendCSItems value, expected boolean. Using default (false)."` warning.
2. Then emits the new `"SendCSItems is disabled."` info line, exactly as it would for an explicit `false`.

**Rationale:** Operators who typo the value get a loud warning *and* a clear statement of what the plugin will actually do (nothing, because the new default is `false`). The "is disabled" line is a single, predictable event the operator can grep for.

**Alternatives considered:**

- *Suppress the info line on invalid input and only show the warning.* Leaves operators guessing what the plugin will do. Rejected.
- *Treat invalid as "not set" (`explicitlySet = false`).* Loses the warning opportunity and silently ignores a typo. Rejected.

### Decision 5: Remove the "Zero Configuration" framing from the README

**Choice:** The README's "Zero Configuration" feature bullet is replaced with "Opt-in forwarding" and the config example is updated to make `"SendCSItems": true` visible (and to call out that omitting it disables forwarding). The "How it works" step 4 is updated from "if the bot's `SendCSItems` config is not `false`" to "if the bot's `SendCSItems` config is `true`".

**Rationale:** The product has changed. Documenting "zero configuration" while the default is `false` would be a lie. The new framing makes the opt-in nature obvious.

### Decision 6: Tests cover the parser; log emission is not unit-tested

**Choice:** Update the existing parser tests to assert the new `enabled == false` default and the new `explicitlySet` flag. Do not add tests for the new `LogGenericInfo` calls — `Bot.ArchiLogger` is not mockable in this project's test setup (no ArchiSteamFarm test seam), and adding one for a single log line is disproportionate.

**Rationale:** The parser is the public surface for unit tests; the logging is glue code that is easy to read and easy to verify manually in a real ASF run. The trade-notification and bot-startup-cs-scan specs add the contract for the logging, and the human maintainer verifies it on a real bot.

**Alternatives considered:**

- *Add a thin abstraction over `ArchiLogger` and inject it.* Real but heavy; out of scope for a default-flip. Rejected.
- *Capture stdout in a test.* The plugin does not write to stdout directly — ASF owns the logger. Rejected.

## Risks / Trade-offs

- **[Silent behavior change for users who relied on the implicit `true` default]** → Mitigation: the README is updated with a prominent opt-in note and the breaking change is called out in the proposal. A future release-notes entry (out of scope) is the user's heads-up.
- **[Operators who set `SendCSItems: true` previously got no log confirmation; now they do — possible noise on bots that have always been enabled]** → Mitigation: a single info line per event (startup + per trade) is acceptable; trade events are infrequent. If it proves noisy, dedup can be added later.
- **[Invalid-value case now produces two log lines (warning + info) instead of one]** → Mitigation: each line says something the other does not. Operators reading top-to-bottom see "value is bad" then "result is disabled" — natural narrative.
- **[Parser signature change touches the only caller and the tests]** → Mitigation: only one production caller (`IsSendCsItemsEnabled`) and the parser test file. Both are updated in the same change. `CSBotConfig` is `internal static`, so this is not a public-API break.
- **[Spec churn across four specs (`cs-inventory-config`, `bot-startup-cs-scan`, `trade-notification`, `plugin-code-organization`)]** → Mitigation: all four edits are part of this change's `tasks.md`, so the contract is updated atomically.

## Migration Plan

1. Land the parser + caller + tests + README + spec edits as a single PR. The plugin version stays at `1.0.0.0` (this is a behavior change inside the existing release, not a version bump).
2. Maintainer writes release notes that call out: "`SendCSItems` now defaults to `false`. To keep forwarding on, set `"SendCSItems": true` in your bot's JSON config. The plugin will log `SendCSItems is enabled.` or `SendCSItems is disabled.` once per relevant event so you can confirm the effective state."
3. No data migration; no config-file rewrites. Users do nothing if they want forwarding OFF (the new default); users who want forwarding ON add one line to their bot's JSON.

**Rollback:** Revert the commit. Behavior returns to default-`true` and silent-on-set, matching the previous release. The legacy `sendcsitems` key (handled in a separate change) is unaffected.

## Open Questions

- Should the new info line be deduplicated per bot per ASF session? *Current decision: no, keep it simple; revisit if operators report noise.*
- Should we add an "explicitly set but invalid" log level above `LogGenericInfo`? *Current decision: no, the existing `LogGenericWarning` for invalid values is enough.*
- Should the README ship a "migration" script that scans a user's bot config directory and inserts `"SendCSItems": true` where the old default was implicitly in effect? *Current decision: no, out of scope; the user is the source of truth for their own config.*
