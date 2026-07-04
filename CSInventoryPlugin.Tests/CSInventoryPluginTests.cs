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
}
