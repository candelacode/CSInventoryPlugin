## MODIFIED Requirements

### Requirement: Declare repository for updates
The plugin SHALL implement `IGitHubPluginUpdates` and declare its `RepositoryName` so ASF can locate the GitHub repository for update checks.

**Updated:** Repository name changed from `BotManagerPlugin` to `CSInventoryPlugin`.

#### Scenario: Plugin declares RepositoryName
- **WHEN** ASF inspects the plugin at startup
- **THEN** `RepositoryName` returns `"candelacode/CSInventoryPlugin"`
- **AND** ASF recognizes the plugin as supporting GitHub-based updates
