using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Exchange;
using JetBrains.Annotations;
using SteamKit2;

namespace CSInventoryPlugin;

[Export(typeof(IPlugin))]
[UsedImplicitly]
public sealed class CSInventoryPlugin : IASF, IBot, IBotConnection, IGitHubPluginUpdates, IBotModules, IBotTradeOfferResults {
	private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, JsonElement>?> BotAdditionalProperties = new();
	private static readonly ConcurrentDictionary<string, bool> BotStartupScanned = new();

	[JsonInclude]
	public string Name => nameof(CSInventoryPlugin);

	public string RepositoryName => "candelacode/CSInventoryPlugin";

	[JsonInclude]
	public Version Version => typeof(CSInventoryPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task OnASFInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
		ASF.ArchiLogger.LogGenericInfo($"{Name} initialized - monitoring for CS items.");
		return Task.CompletedTask;
	}

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo($"{Name} loaded!");
		return Task.CompletedTask;
	}

	public Task OnBotDestroy(Bot bot) {
		ArgumentNullException.ThrowIfNull(bot);

		BotAdditionalProperties.TryRemove(bot.BotName, out _);
		BotStartupScanned.TryRemove(bot.BotName, out _);
		return Task.CompletedTask;
	}

	public Task OnBotInit(Bot bot) => Task.CompletedTask;

	public Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
		ArgumentNullException.ThrowIfNull(bot);

		BotAdditionalProperties[bot.BotName] = additionalConfigProperties;
		return Task.CompletedTask;
	}

	public Task OnBotDisconnected(Bot bot, EResult reason) => Task.CompletedTask;

	public async Task OnBotLoggedOn(Bot bot) {
		ArgumentNullException.ThrowIfNull(bot);

		if (!bot.IsConnectedAndLoggedOn) {
			return;
		}

		if (!IsSendCsItemsEnabled(bot)) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Startup CS item scan skipped (SendCSItems = false).");
			return;
		}

		if (!BotStartupScanned.TryAdd(bot.BotName, true)) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Startup CS item scan already performed, skipping.");
			return;
		}

		await CSItemForwarder.PerformStartupScan(bot).ConfigureAwait(false);
	}

	public async Task OnBotTradeOfferResults(Bot bot, IReadOnlyCollection<ParseTradeResult> tradeResults) {
		if (bot == null || !bot.IsConnectedAndLoggedOn || (tradeResults == null) || (tradeResults.Count == 0)) {
			return;
		}

		if (!IsSendCsItemsEnabled(bot)) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: CS item notification skipped (SendCSItems = false).");
			return;
		}

		var receivedItems = CSItemUtilities.FilterCsItems(
			tradeResults
				.Where(result => result.ItemsToReceive?.Count > 0)
				.SelectMany(result => result.ItemsToReceive!)
		);

		if (receivedItems.Count == 0) {
			return;
		}

		bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Found {receivedItems.Count} CS item(s) received in trade.");
		await CSItemForwarder.ForwardCsItemsToMaster(bot, receivedItems).ConfigureAwait(false);
	}

	private static bool IsSendCsItemsEnabled(Bot bot) {
		BotAdditionalProperties.TryGetValue(bot.BotName, out IReadOnlyDictionary<string, JsonElement>? additionalProperties);

		bool valid = CSBotConfig.TryGetSendCsItems(additionalProperties, out bool enabled);

		if (!valid) {
			bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: Invalid SendCSItems value, expected boolean. Using default (true).");
		}

		return enabled;
	}
}
