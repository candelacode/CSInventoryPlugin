using System.Collections.Generic;
using System.Text.Json;

namespace CSInventoryPlugin;

internal static class CSBotConfig {
	internal static bool TryGetSendCsItems(IReadOnlyDictionary<string, JsonElement>? additionalProperties, out bool enabled) {
		if (additionalProperties == null) {
			enabled = true;
			return true;
		}

		if (!additionalProperties.TryGetValue("SendCSItems", out JsonElement value)) {
			enabled = true;
			return true;
		}

		if (value.ValueKind == JsonValueKind.False) {
			enabled = false;
			return true;
		}

		if (value.ValueKind == JsonValueKind.True) {
			enabled = true;
			return true;
		}

		enabled = true;
		return false;
	}
}
