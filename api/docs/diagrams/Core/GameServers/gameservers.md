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
  GameServerActionDefinition["GameServerActionDefinition"]
  GameServerActionParameter["GameServerActionParameter"]
  GameServerDashboardView["GameServerDashboardView"]
  GameServerDetailsView["GameServerDetailsView"]
  GameServerListRow["GameServerListRow"]
  GameServerLiveCompanion["GameServerLiveCompanion"]
  GameServerLivePlayer["GameServerLivePlayer"]
  GameServerLiveStructure["GameServerLiveStructure"]
  GameServerLiveView["GameServerLiveView"]
  GameServerModEntry["GameServerModEntry"]
  GameServerModSyncDto["GameServerModSyncDto"]
  GameServerModView["GameServerModView"]
  GameServerModsView["GameServerModsView"]
  GameServerStatus["GameServerStatus"]
  GameServerSyncDto["GameServerSyncDto"]
  GameServerSyncEntry["GameServerSyncEntry"]
  GameServerTarget["GameServerTarget"]
  GameServerUpsertResult["GameServerUpsertResult"]
  GameServersSyncReport["GameServersSyncReport"]
  GameServersSyncUseCase["GameServersSyncUseCase"]
  GetGameServerDashboardUseCase["GetGameServerDashboardUseCase"]
  GetGameServerModsUseCase["GetGameServerModsUseCase"]
  GetGameServersUseCase["GetGameServersUseCase"]
  IGameServerActions(["IGameServerActions"])
  IGameServerAssetUrlBuilder(["IGameServerAssetUrlBuilder"])
  IGameServerDashboard(["IGameServerDashboard"])
  IGameServerDashboardRepository(["IGameServerDashboardRepository"])
  IGameServerPollingRepository(["IGameServerPollingRepository"])
  IGameServerProvider(["IGameServerProvider"])
  IGameServerRepository(["IGameServerRepository"])
  IGameServerTargetRepository(["IGameServerTargetRepository"])
  IGameServersManifestProvider(["IGameServersManifestProvider"])
  IModIconResolver(["IModIconResolver"])
  ISteamAppDetailsProvider(["ISteamAppDetailsProvider"])
  PollGameServersUseCase["PollGameServersUseCase"]
  SteamAppDetailsLookup["SteamAppDetailsLookup"]
  end
  subgraph Infrastructure
  ArkProvider["ArkProvider"]
  CobblemonProvider["CobblemonProvider"]
  CurseForgeIconResolver["CurseForgeIconResolver"]
  EnshroudedProvider["EnshroudedProvider"]
  GameServerAssetUrlBuilder["GameServerAssetUrlBuilder"]
  GameServerProtocolConfig["GameServerProtocolConfig"]
  GameServersManifestProvider["GameServersManifestProvider"]
  GameServersOptions["GameServersOptions"]
  GameServersPollingService["GameServersPollingService"]
  HostOverridingGameServerPollingRepository["HostOverridingGameServerPollingRepository"]
  HostOverridingGameServerTargetRepository["HostOverridingGameServerTargetRepository"]
  HumanitzProvider["HumanitzProvider"]
  HumanitzRconClient["HumanitzRconClient"]
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
  CobblemonProvider -.-> IGameServerProvider
  CurseForgeIconResolver -.-> IModIconResolver
  EnshroudedProvider --> SteamA2sClient
  EnshroudedProvider -.-> IGameServerProvider
  GameServerActionDefinition --> GameServerActionParameter
  GameServerAssetUrlBuilder -.-> IGameServerAssetUrlBuilder
  GameServerDashboardController --> GetGameServerDashboardUseCase
  GameServerDetailsView --> GameServerActionDefinition
  GameServerLiveView --> GameServerLivePlayer
  GameServerLiveView --> GameServerLiveStructure
  GameServerModsView --> GameServerModView
  GameServerSyncEntry --> GameServerModEntry
  GameServersController --> GetGameServerModsUseCase
  GameServersController --> GetGameServersUseCase
  GameServersManifestProvider -.-> IGameServersManifestProvider
  GameServersSyncController --> GameServersSyncUseCase
  GameServersSyncUseCase --> IGameServerAssetUrlBuilder
  GameServersSyncUseCase --> IGameServerRepository
  GameServersSyncUseCase --> IGameServersManifestProvider
  GameServersSyncUseCase --> IModIconResolver
  GameServersSyncUseCase --> ISteamAppDetailsProvider
  GetGameServerDashboardUseCase --> IGameServerProvider
  GetGameServerDashboardUseCase --> IGameServerTargetRepository
  GetGameServerModsUseCase --> IGameServerDashboardRepository
  GetGameServersUseCase --> IGameServerDashboardRepository
  GetGameServersUseCase --> IGameServerProvider
  HostOverridingGameServerPollingRepository --> IGameServerPollingRepository
  HostOverridingGameServerTargetRepository --> IGameServerTargetRepository
  HumanitzProvider --> HumanitzRconClient
  HumanitzProvider -.-> IGameServerProvider
  ModrinthIconResolver -.-> IModIconResolver
  PalworldProvider -.-> IGameServerProvider
  PalworldProvider -.-> IGameServerDashboard
  PalworldProvider -.-> IGameServerActions
  PollGameServersUseCase --> IGameServerPollingRepository
  PollGameServersUseCase --> IGameServerProvider
  PostgresGameServerRepository -.-> IGameServerRepository
  PostgresGameServerRepository -.-> IGameServerPollingRepository
  PostgresGameServerRepository -.-> IGameServerDashboardRepository
  PostgresGameServerRepository -.-> IGameServerTargetRepository
  RustProvider --> SteamA2sClient
  RustProvider -.-> IGameServerProvider
  SevenDaysToDieProvider --> SteamA2sClient
  SevenDaysToDieProvider -.-> IGameServerProvider
  SteamAppDetailsProvider -.-> ISteamAppDetailsProvider
```
