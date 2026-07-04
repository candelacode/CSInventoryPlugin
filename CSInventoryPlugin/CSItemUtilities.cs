using System;
using System.Collections.Generic;
using System.Linq;
using ArchiSteamFarm.Steam.Data;

namespace CSInventoryPlugin;

internal static class CSItemUtilities {
	internal const uint CSAppID = 730;
	internal const ulong CSContextID = 2;

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

	internal static HashSet<Asset> FilterCsItems(IEnumerable<Asset> items) {
		ArgumentNullException.ThrowIfNull(items);

		return items.Where(item => item.AppID == CSAppID).ToHashSet();
	}
}
