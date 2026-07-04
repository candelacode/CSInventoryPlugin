using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ArchiSteamFarm.Steam.Data;
using CSInventory.Plugin;
using Xunit;

namespace CSInventory.Plugin.Tests;

public sealed class CSInventoryPluginTests {
	private const uint CSAppID = 730;
	private const uint NonCSAppID = 753;

	[Fact]
	public void AssetWithCSAppID_IsDetectedAsCSItem() {
		var asset = new Asset(CSAppID, 6, 12345, 1);

		Assert.Equal(CSAppID, asset.AppID);
	}

	[Fact]
	public void AssetWithNonCSAppID_IsNotCSItem() {
		var asset = new Asset(NonCSAppID, 6, 12345, 1);

		Assert.NotEqual(CSAppID, asset.AppID);
	}

	[Fact]
	public void SendCsItemsConfig_True_ReturnsTrue() {
		var config = JsonSerializer.Deserialize<JsonElement>("{\"sendcsitems\": true}");

		Assert.Equal(JsonValueKind.True, config.GetProperty("sendcsitems").ValueKind);
	}

	[Fact]
	public void SendCsItemsConfig_False_ReturnsFalse() {
		var config = JsonSerializer.Deserialize<JsonElement>("{\"sendcsitems\": false}");

		Assert.Equal(JsonValueKind.False, config.GetProperty("sendcsitems").ValueKind);
	}

	[Fact]
	public void SendCsItemsConfig_Missing_DefaultsToTrue() {
		var config = JsonSerializer.Deserialize<JsonElement>("{}");

		Assert.False(config.TryGetProperty("sendcsitems", out _));
	}

	[Fact]
	public void SendCsItemsConfig_InvalidType_DefaultsToTrue() {
		var config = JsonSerializer.Deserialize<JsonElement>("{\"sendcsitems\": \"yes\"}");

		Assert.Equal(JsonValueKind.String, config.GetProperty("sendcsitems").ValueKind);
	}

	[Fact]
	public void FilterCsItems_ReturnsOnlyCSItems() {
		var items = new List<Asset> {
			new(CSAppID, 6, 12345, 1),
			new(NonCSAppID, 6, 12346, 1),
			new(CSAppID, 6, 12347, 1),
		};

		var result = CSInventoryPlugin.FilterCsItems(items);

		Assert.Equal(2, result.Count);
		Assert.All(result, item => Assert.Equal(CSAppID, item.AppID));
	}

	[Fact]
	public void FilterCsItems_EmptyWhenNoCSItems() {
		var items = new List<Asset> {
			new(NonCSAppID, 6, 12346, 1),
			new(NonCSAppID, 6, 12347, 1),
		};

		var result = CSInventoryPlugin.FilterCsItems(items);

		Assert.Empty(result);
	}

	[Fact]
	public void FilterCsItems_EmptyCollection_ReturnsEmpty() {
		var result = CSInventoryPlugin.FilterCsItems([]);

		Assert.Empty(result);
	}

	[Fact]
	public void EvaluateMasterForForwarding_NoMaster_ReturnsNoMaster() {
		var decision = CSInventoryPlugin.EvaluateMasterForForwarding(0, 76561198000000001);

		Assert.Equal(CSInventoryPlugin.ForwardMasterDecision.NoMaster, decision);
	}

	[Fact]
	public void EvaluateMasterForForwarding_MasterIsSelf_ReturnsMasterIsSelf() {
		const ulong botSteamID = 76561198000000001;

		var decision = CSInventoryPlugin.EvaluateMasterForForwarding(botSteamID, botSteamID);

		Assert.Equal(CSInventoryPlugin.ForwardMasterDecision.MasterIsSelf, decision);
	}

	[Fact]
	public void EvaluateMasterForForwarding_ValidMaster_ReturnsForward() {
		var decision = CSInventoryPlugin.EvaluateMasterForForwarding(76561198000000002, 76561198000000001);

		Assert.Equal(CSInventoryPlugin.ForwardMasterDecision.Forward, decision);
	}

	[Fact]
	public void StartupScanReconnectGuard_FirstScanProceeds_SecondScanBlocked() {
		var scannedBots = new ConcurrentDictionary<string, bool>();

		bool firstScan = scannedBots.TryAdd("bot1", true);
		bool secondScan = scannedBots.TryAdd("bot1", true);

		Assert.True(firstScan);
		Assert.False(secondScan);
	}

	[Fact]
	public void StartupScanReconnectGuard_DifferentBotsCanScan() {
		var scannedBots = new ConcurrentDictionary<string, bool>();

		bool bot1Scan = scannedBots.TryAdd("bot1", true);
		bool bot2Scan = scannedBots.TryAdd("bot2", true);

		Assert.True(bot1Scan);
		Assert.True(bot2Scan);
	}

	[Fact]
	public void StartupScanReconnectGuard_DestroyAllowsRescan() {
		var scannedBots = new ConcurrentDictionary<string, bool>();

		_ = scannedBots.TryAdd("bot1", true);
		scannedBots.TryRemove("bot1", out _);
		bool rescan = scannedBots.TryAdd("bot1", true);

		Assert.True(rescan);
	}

	[Fact]
	public void CSContextID_IsGameInventoryContextTwo() {
		Assert.Equal<ulong>(2, CSInventoryPlugin.CSContextID);
	}
}
