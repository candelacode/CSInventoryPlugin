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
using ArchiSteamFarm.Storage;
using ArchiSteamFarm.Web.GitHub;
using ArchiSteamFarm.Web.GitHub.Data;
using JetBrains.Annotations;
using SteamKit2;

namespace CSInventory.Plugin;

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

		bool valid = CSBotConfig.TryGetSendCsItems(additionalProperties, out bool enabled, out bool explicitlySet);

		if (!valid) {
			bot.ArchiLogger.LogGenericWarning($"{bot.BotName}: Invalid SendCSItems value, expected boolean. Using default (false).");
		}

		if (explicitlySet) {
			bot.ArchiLogger.LogGenericInfo($"{bot.BotName}: SendCSItems is {(enabled ? "enabled" : "disabled")}.");
		}

		return enabled;
	}

	// Mirrors ArchiSteamFarm/.../IGitHubPluginUpdates.cs:60 and :154. The default interface method in IGitHubPluginUpdates
	// is sealed and the helper it delegates to is also sealed, so we re-implement IPluginUpdates.GetTargetReleaseURL
	// on the class to take precedence over the DIM at dispatch time. The only behavioral difference is that
	// ParseTagAsVersion strips a leading v/V before constructing the System.Version.
	Task<Uri?> IPluginUpdates.GetTargetReleaseURL(Version asfVersion, string asfVariant, bool asfUpdate, GlobalConfig.EUpdateChannel updateChannel, bool forced) {
		ArgumentNullException.ThrowIfNull(asfVersion);
		ArgumentException.ThrowIfNullOrEmpty(asfVariant);

		IGitHubPluginUpdates dim = this;

		if (!dim.CanUpdate) {
			return Task.FromResult<Uri?>(null);
		}

		if (string.IsNullOrEmpty(RepositoryName) || (RepositoryName == "JustArchiNET/ASF-PluginTemplate")) {
			ASF.ArchiLogger.LogGenericError($"Plugin update failed: RepositoryName is not set.");

			return Task.FromResult<Uri?>(null);
		}

		return GetTargetReleaseURLInternalAsync(asfVersion, asfVariant, asfUpdate, updateChannel == GlobalConfig.EUpdateChannel.Stable, forced);
	}

	private async Task<Uri?> GetTargetReleaseURLInternalAsync(Version asfVersion, string asfVariant, bool asfUpdate, bool stable, bool forced) {
		ArgumentNullException.ThrowIfNull(asfVersion);
		ArgumentException.ThrowIfNullOrEmpty(asfVariant);

		ReleaseResponse? releaseResponse = await GitHubService.GetLatestRelease(RepositoryName, stable).ConfigureAwait(false);

		if (releaseResponse == null) {
			return null;
		}

		Version newVersion = ParseTagAsVersion(releaseResponse.Tag);

		if (!forced && (Version >= newVersion)) {
			// Start from evaluating whether the version is the same and we're actually updating ASF as part of this call
			// Then calculate assets that can possibly take part in the update process, in order to determine whether the change of plugin variant is possible
			// The base condition is that the release must have at least 2 total assets, therefore we need to only take into account GetPossibleMatchesByName() logic, while assuming that version is flexible
			// If by the end we have at least 2 assets we're considering for an update, then that's a possible variant change and in this case we should proceed to cover for the edge case explained above
			if ((Version > newVersion) || !asfUpdate || (releaseResponse.Assets.Count < 2) || GetPossibleNames().All(pluginName => releaseResponse.Assets.Count(asset => asset.Name.Equals($"{pluginName}.zip", StringComparison.OrdinalIgnoreCase) || (asset.Name.StartsWith($"{pluginName}-V", StringComparison.OrdinalIgnoreCase) && asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))) < 2)) {
				ASF.ArchiLogger.LogGenericInfo($"No plugin update found (current: {Version}, remote: {newVersion}).");

				return null;
			}
		}

		if (releaseResponse.Assets.Count == 0) {
			ASF.ArchiLogger.LogGenericWarning($"Plugin update failed: no usable asset found in release {newVersion}.");

			return null;
		}

		IGitHubPluginUpdates dim = this;
		ReleaseAsset? asset = await dim.GetTargetReleaseAsset(asfVersion, asfVariant, newVersion, releaseResponse.Assets).ConfigureAwait(false);

		if ((asset == null) || !releaseResponse.Assets.Contains(asset)) {
			ASF.ArchiLogger.LogGenericWarning($"Plugin update failed: no usable asset found in release {newVersion}.");

			return null;
		}

		ASF.ArchiLogger.LogGenericInfo($"Plugin update found (current: {Version}, remote: {newVersion}).");

		return asset.DownloadURL;

		IEnumerable<string> GetPossibleNames() {
			string pluginName = Name;

			if (!string.IsNullOrEmpty(pluginName)) {
				yield return pluginName;
			}

			string? assemblyName = GetType().Assembly.GetName().Name;

			if (!string.IsNullOrEmpty(assemblyName) && !assemblyName.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) {
				yield return assemblyName;
			}
		}
	}

	internal static Version ParseTagAsVersion(string tag) {
		if (!string.IsNullOrEmpty(tag) && ((tag[0] == 'v') || (tag[0] == 'V'))) {
			tag = tag.Substring(1);
		}

		int dashIndex = tag.IndexOf('-', StringComparison.Ordinal);
		if (dashIndex >= 0) {
			tag = tag.Substring(0, dashIndex);
		}

		string[] parts = tag.Split('.');
		if (parts.Length < 4) {
			string[] padded = new string[4];
			for (int i = 0; i < 4; i++) {
				padded[i] = i < parts.Length ? parts[i] : "0";
			}
			tag = string.Join(".", padded);
		}

		return new Version(tag);
	}
}
