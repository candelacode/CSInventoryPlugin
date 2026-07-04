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

namespace CSInventory.Plugin;

[Export(typeof(IPlugin))]
[UsedImplicitly]
public sealed class CSInventoryPlugin : IASF, IBot, IBotConnection, IGitHubPluginUpdates, IBotModules, IBotTradeOfferResults {
	private const uint CSAppID = 730;
	internal const ulong CSContextID = 2;

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

		if (!GetSendCsItemsConfig(bot)) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Startup CS item scan skipped (sendcsitems = false).");
			return;
		}

		if (!BotStartupScanned.TryAdd(bot.BotName, true)) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Startup CS item scan already performed, skipping.");
			return;
		}

		bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Performing startup CS item scan.");

		(HashSet<Asset>? inventory, string inventoryMessage) = await bot.Actions.GetInventory(appID: CSAppID, contextID: CSContextID).ConfigureAwait(false);

		if (inventory == null || inventory.Count == 0) {
			if (inventory == null) {
				bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: Startup CS item scan failed to load inventory: {inventoryMessage}");
			}

			return;
		}

		bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Found {inventory.Count} CS item(s) in startup inventory scan.");
		await ForwardCsItemsToMaster(bot, inventory).ConfigureAwait(false);
	}

	public async Task OnBotTradeOfferResults(Bot bot, IReadOnlyCollection<ParseTradeResult> tradeResults) {
		if (bot == null || !bot.IsConnectedAndLoggedOn || (tradeResults == null) || (tradeResults.Count == 0)) {
			return;
		}

		if (!GetSendCsItemsConfig(bot)) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: CS item notification skipped (sendcsitems = false).");
			return;
		}

		var receivedItems = FilterCsItems(
			tradeResults
				.Where(result => result.ItemsToReceive?.Count > 0)
				.SelectMany(result => result.ItemsToReceive!)
		);

		if (receivedItems.Count == 0) {
			return;
		}

		bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Found {receivedItems.Count} CS item(s) received in trade.");
		await ForwardCsItemsToMaster(bot, receivedItems).ConfigureAwait(false);
	}

	private static async Task ForwardCsItemsToMaster(Bot bot, HashSet<Asset> csItems) {
		ArgumentNullException.ThrowIfNull(bot);
		ArgumentNullException.ThrowIfNull(csItems);

		if (csItems.Count == 0) {
			return;
		}

		ulong masterSteamID = bot.Actions.GetFirstSteamMasterID();
		if (!ShouldForwardToMaster(masterSteamID, bot.SteamID, out string? skipReason)) {
			bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: {skipReason}");
			return;
		}

		var result = await bot.Actions.SendInventory(csItems, masterSteamID).ConfigureAwait(false);

		if (result.Success) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: CS items forwarded to {masterSteamID} successfully.");
		} else {
			bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: Failed to forward CS items to {masterSteamID}: {result.Message}");
		}
	}

	internal enum ForwardMasterDecision {
		Forward,
		NoMaster,
		MasterIsSelf
	}

	internal static ForwardMasterDecision EvaluateMasterForForwarding(ulong masterSteamID, ulong botSteamID) {
		if (masterSteamID == 0) {
			return ForwardMasterDecision.NoMaster;
		}

		if (masterSteamID == botSteamID) {
			return ForwardMasterDecision.MasterIsSelf;
		}

		return ForwardMasterDecision.Forward;
	}

	private static bool ShouldForwardToMaster(ulong masterSteamID, ulong botSteamID, out string? skipReason) {
		switch (EvaluateMasterForForwarding(masterSteamID, botSteamID)) {
			case ForwardMasterDecision.NoMaster:
				skipReason = "No master account configured, cannot forward CS items.";
				return false;
			case ForwardMasterDecision.MasterIsSelf:
				skipReason = "Master account is the bot itself, skipping CS item trade.";
				return false;
			default:
				skipReason = null;
				return true;
		}
	}

	internal static HashSet<Asset> FilterCsItems(IEnumerable<Asset> items) {
		ArgumentNullException.ThrowIfNull(items);

		return items.Where(item => item.AppID == CSAppID).ToHashSet();
	}

	private static bool GetSendCsItemsConfig(Bot bot) {
		if (!BotAdditionalProperties.TryGetValue(bot.BotName, out IReadOnlyDictionary<string, JsonElement>? additionalProperties) || (additionalProperties == null)) {
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
