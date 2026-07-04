## Why

The plugin's entire logic lives in a single 200-line `CSInventoryPlugin.cs` file that mixes ASF lifecycle callbacks, CS item filtering, config parsing, master validation, and trade forwarding. This makes the code harder to navigate, test, and maintain. The official ASF plugins (e.g. `ItemsMatcher`) follow a better pattern: a slim entry-point class that delegates to separate static utility and logic files. Refactoring now — while the codebase is still small — establishes a clean structure before future features add more complexity.

## What Changes

- Split `CSInventoryPlugin.cs` into separate files, each with a single responsibility:
  - `CSInventoryPlugin.cs` — slim plugin entry point containing only ASF interface implementations (lifecycle callbacks), delegating to the new modules.
  - `CSItemUtilities.cs` — internal static utility class for pure CS item logic (filtering, master validation).
  - `CSBotConfig.cs` — per-bot config parsing for `sendcsitems`, decoupled from `Bot` so it takes a raw dictionary and returns a parsed result.
  - `CSItemForwarder.cs` — forwarding and startup-scan logic (fetch inventory, validate master, send trade offer, log results).
- Move CS-specific constants (`CSAppID`, `CSContextID`) into `CSItemUtilities` where they are used.
- Improve test coverage by testing the now-decoupled config parsing without a `Bot` instance.
- Remove tests that only test framework behavior (e.g. `ConcurrentDictionary.TryAdd`) and replace them with tests that test our plugin's actual logic through the new module APIs.
- No behavior changes — the plugin's external behavior remains identical.

## Capabilities

### New Capabilities
- `plugin-code-organization`: Documents the file-level structure and separation-of-concerns requirements for the plugin source code, ensuring it follows ASF official plugin patterns.

### Modified Capabilities

(none — existing behavioral specs remain unchanged)

## Impact

- **Code**: `CSInventoryPlugin/CSInventoryPlugin.cs` is split into 4 files; `CSInventoryPlugin.Tests/CSInventoryPluginTests.cs` is updated to test the new module APIs.
- **Specs**: No spec changes — all existing specs (`bot-startup-cs-scan`, `trade-notification`, `cs-inventory-config`, `cs-item-monitor`, `github-autoupdate`, `asf-plugin-development`) remain valid as requirements are unchanged.
- **Dependencies**: No new NuGet packages; same ASF API surface.
- **Compatibility**: No breaking changes; the plugin's compiled output and behavior are identical.
- **Build**: `InternalsVisibleTo` already configured for tests; new internal classes are accessible.
