using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using JetBrains.Annotations;

namespace CSInventory.Plugin;

[UsedImplicitly]
internal static class CSItemForwarder {
	internal static async Task ForwardCsItemsToMaster(Bot bot, HashSet<Asset> csItems) {
		ArgumentNullException.ThrowIfNull(bot);
		ArgumentNullException.ThrowIfNull(csItems);

		if (csItems.Count == 0) {
			return;
		}

		ulong masterSteamID = bot.Actions.GetFirstSteamMasterID();
		CSItemUtilities.ForwardMasterDecision decision = CSItemUtilities.EvaluateMasterForForwarding(masterSteamID, bot.SteamID);

		if (decision != CSItemUtilities.ForwardMasterDecision.Forward) {
			string skipReason = decision switch {
				CSItemUtilities.ForwardMasterDecision.NoMaster => "No master account configured, cannot forward CS items.",
				CSItemUtilities.ForwardMasterDecision.MasterIsSelf => "Master account is the bot itself, skipping CS item trade.",
				_ => throw new InvalidOperationException(nameof(decision))
			};

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

	internal static async Task PerformStartupScan(Bot bot) {
		ArgumentNullException.ThrowIfNull(bot);

		bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Performing startup CS item scan.");

		(HashSet<Asset>? inventory, string inventoryMessage) = await bot.Actions.GetInventory(appID: CSItemUtilities.CSAppID, contextID: CSItemUtilities.CSContextID).ConfigureAwait(false);

		if (inventory == null) {
			bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: Startup CS item scan failed to load inventory: {inventoryMessage}");
			return;
		}

		if (inventory.Count == 0) {
			return;
		}

		bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: Found {inventory.Count} CS item(s) in startup inventory scan.");
		await ForwardCsItemsToMaster(bot, inventory).ConfigureAwait(false);
	}
}
