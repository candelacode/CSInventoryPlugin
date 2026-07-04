using System;
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

namespace CSInventory.Plugin;

[Export(typeof(IPlugin))]
[UsedImplicitly]
public sealed class CSInventoryPlugin : IASF, IGitHubPluginUpdates, IBotTradeOfferResults {
	private const uint CSAppID = 730;

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

	public async Task OnBotTradeOfferResults(Bot bot, IReadOnlyCollection<ParseTradeResult> tradeResults) {
		if (bot == null || !bot.IsConnectedAndLoggedOn || (tradeResults == null) || (tradeResults.Count == 0)) {
			return;
		}

		if (!GetSendCsItemsConfig(bot)) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: CS item notification skipped (sendcsitems = false).");
			return;
		}

		var receivedItems = tradeResults
			.Where(result => result.ItemsToReceive?.Count > 0)
			.SelectMany(result => result.ItemsToReceive!)
			.Where(item => item.AppID == CSAppID)
			.ToHashSet();

		if (receivedItems.Count == 0) {
			return;
		}

		bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Found {receivedItems.Count} CS item(s) received in trade.");

		ulong masterSteamID = bot.Actions.GetFirstSteamMasterID();
		if (masterSteamID == 0) {
			bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: No master account configured, cannot forward CS items.");
			return;
		}

		if (masterSteamID == bot.SteamID) {
			bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: Master account is the bot itself, skipping CS item trade.");
			return;
		}

		var result = await bot.Actions.SendInventory(receivedItems, masterSteamID).ConfigureAwait(false);

		if (result.Success) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: CS items forwarded to {masterSteamID} successfully.");
		} else {
			bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: Failed to forward CS items to {masterSteamID}: {result.Message}");
		}
	}

	private static bool GetSendCsItemsConfig(Bot bot) {
		var additionalProperties = bot.BotConfig.AdditionalProperties;
		if (additionalProperties == null) {
			return true;
		}

		if (additionalProperties.TryGetValue("sendcsitems", out JsonElement value)) {
			if (value.ValueKind == JsonValueKind.False) {
				return false;
			}

			if (value.ValueKind != JsonValueKind.True) {
				bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: Invalid sendcsitems value '{value}', expected boolean. Using default (true).");
			}
		}

		return true;
	}
}
