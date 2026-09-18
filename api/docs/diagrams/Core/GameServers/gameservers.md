# Core / GameServers

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  GameServerDashboardController["GameServerDashboardController"]
  GameServersController["GameServersController"]
  GameServersSyncController["GameServersSyncController"]
  end
  subgraph Application
  ExecuteGameServerActionRequest["ExecuteGameServerActionRequest"]
  GameServerActionDefinition["GameServerActionDefinition"]
  GameServerActionParameter["GameServerActionParameter"]
  GameServerDashboardView["GameServerDashboardView"]
  GameServerDetailsView["GameServerDetailsView"]
  GameServerListRow["GameServerListRow"]
  GameServerLiveCompanion["GameServerLiveCompanion"]
  GameServerLivePlayer["GameServerLivePlayer"]
  GameServerLiveSnapshot["GameServerLiveSnapshot"]
  GameServerLiveStructure["GameServerLiveStructure"]
  GameServerLiveView["GameServerLiveView"]
  GameServerModEntry["GameServerModEntry"]
  GameServerModSyncDto["GameServerModSyncDto"]
  GameServerModView["GameServerModView"]
  GameServerModsView["GameServerModsView"]
  GameServerRawCommandHistoryEntry["GameServerRawCommandHistoryEntry"]
  GameServerRawCommandRequest["GameServerRawCommandRequest"]
  GameServerRawCommandResult["GameServerRawCommandResult"]
  GameServerStatus["GameServerStatus"]
  GameServerSyncDto["GameServerSyncDto"]
  GameServerSyncEntry["GameServerSyncEntry"]
  GameServerTarget["GameServerTarget"]
  GameServerUpsertResult["GameServerUpsertResult"]
  GameServersSyncReport["GameServersSyncReport"]
  GameServersSyncUseCase["GameServersSyncUseCase"]
  GetGameServerDashboardUseCase["GetGameServerDashboardUseCase"]
  GetGameServerModsUseCase["GetGameServerModsUseCase"]
  GetGameServersLiveStateUseCase["GetGameServersLiveStateUseCase"]
  GetGameServersUseCase["GetGameServersUseCase"]
  IGameServerActionCountdownService(["IGameServerActionCountdownService"])
  IGameServerActions(["IGameServerActions"])
  IGameServerAssetUrlBuilder(["IGameServerAssetUrlBuilder"])
  IGameServerDashboard(["IGameServerDashboard"])
  IGameServerDashboardRepository(["IGameServerDashboardRepository"])
  IGameServerLiveStateStore(["IGameServerLiveStateStore"])
  IGameServerPollingRepository(["IGameServerPollingRepository"])
  IGameServerProvider(["IGameServerProvider"])
  IGameServerRawCommand(["IGameServerRawCommand"])
  IGameServerRawCommandHistoryRepository(["IGameServerRawCommandHistoryRepository"])
  IGameServerRepository(["IGameServerRepository"])
  IGameServerTargetRepository(["IGameServerTargetRepository"])
  IGameServersManifestProvider(["IGameServersManifestProvider"])
  IModIconResolver(["IModIconResolver"])
  ISteamAppDetailsProvider(["ISteamAppDetailsProvider"])
  PollGameServersUseCase["PollGameServersUseCase"]
  ScheduledGameServerAction["ScheduledGameServerAction"]
  SteamAppDetailsLookup["SteamAppDetailsLookup"]
  end
  subgraph Infrastructure
  ArkProvider["ArkProvider"]
  CobblemonProvider["CobblemonProvider"]
  CurseForgeIconResolver["CurseForgeIconResolver"]
  EnshroudedProvider["EnshroudedProvider"]
  GameServerActionCountdownService["GameServerActionCountdownService"]
  GameServerAssetUrlBuilder["GameServerAssetUrlBuilder"]
  GameServerProtocolConfig["GameServerProtocolConfig"]
  GameServersManifestProvider["GameServersManifestProvider"]
  GameServersOptions["GameServersOptions"]
  GameServersPollingService["GameServersPollingService"]
  HostOverridingGameServerPollingRepository["HostOverridingGameServerPollingRepository"]
  HostOverridingGameServerTargetRepository["HostOverridingGameServerTargetRepository"]
  HumanitzProvider["HumanitzProvider"]
  HumanitzRconClient["HumanitzRconClient"]
  InMemoryGameServerLiveStateStore["InMemoryGameServerLiveStateStore"]
  ModrinthIconResolver["ModrinthIconResolver"]
  PalworldProvider["PalworldProvider"]
  PostgresGameServerRepository["PostgresGameServerRepository"]
  RustProvider["RustProvider"]
  SevenDaysToDieProvider["SevenDaysToDieProvider"]
  SourceRconClient["SourceRconClient"]
  SteamA2sClient["SteamA2sClient"]
  SteamAppDetailsProvider["SteamAppDetailsProvider"]
  end
  subgraph Autre
  GameServersModule["GameServersModule"]
  end
  ArkProvider -.-> IGameServerProvider
  ArkProvider -.-> IGameServerDashboard
  ArkProvider -.-> IGameServerActions
  ArkProvider -.-> IGameServerRawCommand
  CobblemonProvider -.-> IGameServerProvider
  CobblemonProvider -.-> IGameServerDashboard
  CobblemonProvider -.-> IGameServerActions
  CobblemonProvider -.-> IGameServerRawCommand
  CurseForgeIconResolver -.-> IModIconResolver
  EnshroudedProvider --> SteamA2sClient
  EnshroudedProvider -.-> IGameServerProvider
  GameServerActionCountdownService -.-> IGameServerActionCountdownService
  GameServerActionDefinition --> GameServerActionParameter
  GameServerAssetUrlBuilder -.-> IGameServerAssetUrlBuilder
  GameServerDashboardController --> GetGameServerDashboardUseCase
  GameServerDetailsView --> GameServerActionDefinition
  GameServerLiveView --> GameServerLivePlayer
  GameServerLiveView --> GameServerLiveStructure
  GameServerModsView --> GameServerModView
  GameServerSyncEntry --> GameServerModEntry
  GameServersController --> GetGameServerModsUseCase
  GameServersController --> GetGameServersLiveStateUseCase
  GameServersController --> GetGameServersUseCase
  GameServersManifestProvider -.-> IGameServersManifestProvider
  GameServersSyncController --> GameServersSyncUseCase
  GameServersSyncUseCase --> IGameServerAssetUrlBuilder
  GameServersSyncUseCase --> IGameServerRepository
  GameServersSyncUseCase --> IGameServersManifestProvider
  GameServersSyncUseCase --> IModIconResolver
  GameServersSyncUseCase --> ISteamAppDetailsProvider
  GetGameServerDashboardUseCase --> IGameServerActionCountdownService
  GetGameServerDashboardUseCase --> IGameServerProvider
  GetGameServerDashboardUseCase --> IGameServerRawCommandHistoryRepository
  GetGameServerDashboardUseCase --> IGameServerTargetRepository
  GetGameServerDashboardUseCase --> PollGameServersUseCase
  GetGameServerModsUseCase --> IGameServerDashboardRepository
  GetGameServersLiveStateUseCase --> IGameServerLiveStateStore
  GetGameServersUseCase --> IGameServerDashboardRepository
  GetGameServersUseCase --> IGameServerProvider
  HostOverridingGameServerPollingRepository --> IGameServerPollingRepository
  HostOverridingGameServerTargetRepository --> IGameServerTargetRepository
  HumanitzProvider --> HumanitzRconClient
  HumanitzProvider -.-> IGameServerProvider
  InMemoryGameServerLiveStateStore -.-> IGameServerLiveStateStore
  ModrinthIconResolver -.-> IModIconResolver
  PalworldProvider -.-> IGameServerProvider
  PalworldProvider -.-> IGameServerDashboard
  PalworldProvider -.-> IGameServerActions
  PollGameServersUseCase --> IGameServerLiveStateStore
  PollGameServersUseCase --> IGameServerPollingRepository
  PollGameServersUseCase --> IGameServerProvider
  PostgresGameServerRepository -.-> IGameServerRepository
  PostgresGameServerRepository -.-> IGameServerPollingRepository
  PostgresGameServerRepository -.-> IGameServerDashboardRepository
  PostgresGameServerRepository -.-> IGameServerTargetRepository
  PostgresGameServerRepository -.-> IGameServerRawCommandHistoryRepository
  RustProvider --> SteamA2sClient
  RustProvider -.-> IGameServerProvider
  ScheduledGameServerAction --> GameServerTarget
  SevenDaysToDieProvider --> SteamA2sClient
  SevenDaysToDieProvider -.-> IGameServerProvider
  SteamAppDetailsProvider -.-> ISteamAppDetailsProvider
```
