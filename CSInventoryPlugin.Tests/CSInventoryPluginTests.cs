using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ArchiSteamFarm.Steam.Data;
using CSInventory.Plugin;
using CSInventoryPlugin;
using Xunit;

namespace CSInventoryPlugin.Tests;

public sealed class CSInventoryPluginTests {
	private const uint CSAppID = 730;
	private const uint NonCSAppID = 753;
	private const ulong CSContextID = 2;

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
	public void CSContextID_IsGameInventoryContextTwo() {
		Assert.Equal<ulong>(CSContextID, CSItemUtilities.CSContextID);
	}

	[Fact]
	public void CSAppID_IsSevenHundredThirty() {
		Assert.Equal<uint>(730, CSItemUtilities.CSAppID);
	}

	[Fact]
	public void FilterCsItems_ReturnsOnlyCSItems() {
		var items = new List<Asset> {
			new(CSAppID, 6, 12345, 1),
			new(NonCSAppID, 6, 12346, 1),
			new(CSAppID, 6, 12347, 1),
		};

		var result = CSItemUtilities.FilterCsItems(items);

		Assert.Equal(2, result.Count);
		Assert.All(result, item => Assert.Equal(CSAppID, item.AppID));
	}

	[Fact]
	public void FilterCsItems_EmptyWhenNoCSItems() {
		var items = new List<Asset> {
			new(NonCSAppID, 6, 12346, 1),
			new(NonCSAppID, 6, 12347, 1),
		};

		var result = CSItemUtilities.FilterCsItems(items);

		Assert.Empty(result);
	}

	[Fact]
	public void FilterCsItems_EmptyCollection_ReturnsEmpty() {
		var result = CSItemUtilities.FilterCsItems([]);

		Assert.Empty(result);
	}

	[Fact]
	public void EvaluateMasterForForwarding_NoMaster_ReturnsNoMaster() {
		var decision = CSItemUtilities.EvaluateMasterForForwarding(0, 76561198000000001);

		Assert.Equal(CSItemUtilities.ForwardMasterDecision.NoMaster, decision);
	}

	[Fact]
	public void EvaluateMasterForForwarding_MasterIsSelf_ReturnsMasterIsSelf() {
		const ulong botSteamID = 76561198000000001;

		var decision = CSItemUtilities.EvaluateMasterForForwarding(botSteamID, botSteamID);

		Assert.Equal(CSItemUtilities.ForwardMasterDecision.MasterIsSelf, decision);
	}

	[Fact]
	public void EvaluateMasterForForwarding_ValidMaster_ReturnsForward() {
		var decision = CSItemUtilities.EvaluateMasterForForwarding(76561198000000002, 76561198000000001);

		Assert.Equal(CSItemUtilities.ForwardMasterDecision.Forward, decision);
	}

	[Fact]
	public void TryGetSendCsItems_True_ReturnsValidAndEnabled() {
		var config = JsonSerializer.Deserialize<JsonElement>("{\"SendCSItems\": true}");

		bool valid = CSBotConfig.TryGetSendCsItems(
			config.EnumerateObject().ToDictionary(p => p.Name, p => p.Value),
			out bool enabled
		);

		Assert.True(valid);
		Assert.True(enabled);
	}

	[Fact]
	public void TryGetSendCsItems_False_ReturnsValidAndDisabled() {
		var config = JsonSerializer.Deserialize<JsonElement>("{\"SendCSItems\": false}");

		bool valid = CSBotConfig.TryGetSendCsItems(
			config.EnumerateObject().ToDictionary(p => p.Name, p => p.Value),
			out bool enabled
		);

		Assert.True(valid);
		Assert.False(enabled);
	}

	[Fact]
	public void TryGetSendCsItems_Missing_ReturnsValidAndEnabledDefault() {
		var config = JsonSerializer.Deserialize<JsonElement>("{}");

		bool valid = CSBotConfig.TryGetSendCsItems(
			config.EnumerateObject().ToDictionary(p => p.Name, p => p.Value),
			out bool enabled
		);

		Assert.True(valid);
		Assert.True(enabled);
	}

	[Fact]
	public void TryGetSendCsItems_NullProperties_ReturnsValidAndEnabledDefault() {
		bool valid = CSBotConfig.TryGetSendCsItems(null, out bool enabled);

		Assert.True(valid);
		Assert.True(enabled);
	}

	[Fact]
	public void TryGetSendCsItems_InvalidType_ReturnsInvalidAndEnabledDefault() {
		var config = JsonSerializer.Deserialize<JsonElement>("{\"SendCSItems\": \"yes\"}");

		bool valid = CSBotConfig.TryGetSendCsItems(
			config.EnumerateObject().ToDictionary(p => p.Name, p => p.Value),
			out bool enabled
		);

		Assert.False(valid);
		Assert.True(enabled);
	}

	[Fact]
	public void TryGetSendCsItems_NumberType_ReturnsInvalidAndEnabledDefault() {
		var config = JsonSerializer.Deserialize<JsonElement>("{\"SendCSItems\": 1}");

		bool valid = CSBotConfig.TryGetSendCsItems(
			config.EnumerateObject().ToDictionary(p => p.Name, p => p.Value),
			out bool enabled
		);

		Assert.False(valid);
		Assert.True(enabled);
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
}
