## Context

CSInventoryPlugin is an ASF plugin that monitors trade results for Counter-Strike items and forwards them to the bot's master account. The current implementation directly accesses `BotConfig.AdditionalProperties`, which is marked `internal` in ASF and therefore inaccessible from external plugins.

The change replaces direct property access with the ASF plugin API (`IBotModules.OnBotInitModules`), which provides per-bot additional config properties via a public interface.

## Goals / Non-Goals

**Goals:**
- Fix compilation error by using `IBotModules` API instead of direct `internal` property access
- Detect CS items (appId 730) from trade result callbacks
- Forward detected CS items to the bot's master account via trade offer
- Support per-bot opt-out via `sendcsitems` config property

**Non-Goals:**
- No deduplication of items already in master's inventory
- No retry logic for failed trade offers
- No UI or IPC endpoints for this feature
- No CS item detection outside of trade result callbacks (e.g., inventory polling)

## Decisions

1. **Use `IBotModules` for per-bot config** instead of `BotConfig.AdditionalProperties` (which is `internal`). ASF calls `OnBotInitModules` during bot initialization with the additional properties dictionary, which is the official plugin API for accessing extension data.

2. **Store per-bot properties in a `ConcurrentDictionary`** keyed by `bot.BotName`. This avoids accessing `internal` members while providing the same data at trade-check time.

3. **Use `bot.Actions.SendInventory()`** to forward items. This is the standard ASF API for creating trade offers and handles trade creation, confirmation, and sending.

4. **Single plugin class (`CSInventoryPlugin`)** implementing multiple interfaces (`IASF`, `IBotModules`, `IBotTradeOfferResults`, `IGitHubPluginUpdates`) to keep the deployment simple — one DLL in the plugins folder.

## Risks / Trade-offs

- `OnBotInitModules` is called once per bot at startup. If the bot config changes at runtime, the cached dictionary won't reflect it. This is acceptable because ASF requires a restart for config changes.
- No retry on failed trade sends. Trade failures are logged but not retried automatically. The user would need to trigger a new trade via normal gameplay.
