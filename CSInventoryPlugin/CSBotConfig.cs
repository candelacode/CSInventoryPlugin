using System.Collections.Generic;
using System.Text.Json;

namespace CSInventory.Plugin;

internal static class CSBotConfig {
	internal static bool TryGetSendCsItems(IReadOnlyDictionary<string, JsonElement>? additionalProperties, out bool enabled, out bool explicitlySet) {
		if (additionalProperties == null) {
			enabled = false;
			explicitlySet = false;
			return true;
		}

		if (!additionalProperties.TryGetValue("SendCSItems", out JsonElement value)) {
			enabled = false;
			explicitlySet = false;
			return true;
		}

		if (value.ValueKind == JsonValueKind.False) {
			enabled = false;
			explicitlySet = true;
			return true;
		}

		if (value.ValueKind == JsonValueKind.True) {
			enabled = true;
			explicitlySet = true;
			return true;
		}

		enabled = false;
		explicitlySet = true;
		return false;
	}
}
