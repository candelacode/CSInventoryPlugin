## Context

The project is currently `BotManagerPlugin` — an ASF plugin with REST API endpoints for enabling/disabling bots. It uses `BotManagementPlugin.cs` as the entry point, `BotManagerController.cs` for REST APIs, and `BotManagementService.cs` for bot management logic. The solution includes a test project `BotManagerPlugin.Tests`.

The goal is to completely repurpose this into `CSInventoryPlugin` — an ASF plugin that monitors bot inventories for Counter Strike items and sends trade notifications to a master account.

## Goals / Non-Goals

**Goals:**
- Rename solution, project folders, namespaces, and assemblies from `BotManagerPlugin` to `CSInventoryPlugin`
- Remove all REST API bot management code (controller, service)
- Implement CS inventory monitoring on bot inventory changes
- Implement trade notification sending to the configured master account
- Support per-bot `sendcsitems` config property (default true)
- Update GitHub autoupdate metadata to match new repo name

**Non-Goals:**
- Not removing the old BotManagerPlugin test project structure (it will be refactored to test new functionality)
- Not adding external dependencies beyond what ASF provides
- Not implementing a persistent database or store for trade history
- Not handling non-CS Steam app items

## Decisions

1. **Decision**: Use `IBotTradeOfferResults` ASF plugin interface for detecting inventory changes
   - **Rationale**: This interface fires after trade offer processing, giving us access to received items. Alternative was polling inventory periodically, but event-driven is more efficient and timely.
   - **Alternatives considered**: Polling via `ITimer` — wasteful for always-on ASF; `IBotTradeOfferResults` is the cleanest hook.

2. **Decision**: Query inventory via `Bot.SteamInventory` after trade events
   - **Rationale**: ASF exposes `Bot.SteamInventory.GetCache()` for cached inventory. After a trade, we can check for CS items (appId 730) in the cached inventory.
   - **Alternatives considered**: Using Steam API directly — unnecessary when ASF already maintains inventory cache.

3. **Decision**: Send notifications via `Bot.SendTradeOffer()` with CS items
   - **Rationale**: The simplest way to forward items to master is using ASF's own trade sending mechanism. We create a trade offer from the bot to the master account containing the CS items.
   - **Alternatives considered**: External notification (Discord/Telegram) — out of scope; IPC endpoint — too indirect.

4. **Decision**: Per-bot config via `BotConfig.SendCsItems` mapped from `sendcsitems` JSON property
   - **Rationale**: ASF plugin config properties are accessed via `BotConfig.GetAdditionalConfigProperties()`. We'll read the `sendcsitems` key from the bot's additional config.
   - **Alternatives considered**: Separate config file — unnecessary complexity; environment variable — not per-bot.

5. **Decision**: `Directory.Build.props` stays at root but renamed properties for `CSInventoryPlugin`
   - **Rationale**: The project structure (submodule, solution file, build props) is sound — only assembly name and metadata need updating.

## Risks / Trade-offs

- **[Risk] CS items not detected if inventory cache is stale**: ASF's `SteamInventory.GetCache()` may not reflect the very latest state after a trade. **Mitigation**: Call `SteamInventory.RequestCacheAsync()` to refresh before checking.
- **[Risk] Trade offer limits**: ASF may have rate limits or offer limits that prevent sending trades. **Mitigation**: Let ASF handle rate limiting natively; log failures gracefully.
- **[Trade-off] Event-driven vs polling**: Event-driven only catches items after trades, not items already in inventory when plugin loads. **Mitigation**: Acceptable — plugin monitors new activity, not historical inventory.
