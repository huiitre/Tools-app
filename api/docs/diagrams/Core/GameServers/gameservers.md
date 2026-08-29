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
  GameServerStatus["GameServerStatus"]
  GameServerSyncDto["GameServerSyncDto"]
  GameServerSyncEntry["GameServerSyncEntry"]
  GameServerTarget["GameServerTarget"]
  GameServerUpsertResult["GameServerUpsertResult"]
  GameServersSyncReport["GameServersSyncReport"]
  GameServersSyncUseCase["GameServersSyncUseCase"]
  GetGameServerDashboardUseCase["GetGameServerDashboardUseCase"]
  GetGameServersUseCase["GetGameServersUseCase"]
  IGameServerActions(["IGameServerActions"])
  IGameServerDashboard(["IGameServerDashboard"])
  IGameServerDashboardRepository(["IGameServerDashboardRepository"])
  IGameServerImageUrlBuilder(["IGameServerImageUrlBuilder"])
  IGameServerPollingRepository(["IGameServerPollingRepository"])
  IGameServerProvider(["IGameServerProvider"])
  IGameServerRepository(["IGameServerRepository"])
  IGameServerTargetRepository(["IGameServerTargetRepository"])
  IGameServersManifestProvider(["IGameServersManifestProvider"])
  ISteamAppDetailsProvider(["ISteamAppDetailsProvider"])
  PollGameServersUseCase["PollGameServersUseCase"]
  SteamAppDetailsLookup["SteamAppDetailsLookup"]
  end
  subgraph Infrastructure
  ArkProvider["ArkProvider"]
  GameServerImageUrlBuilder["GameServerImageUrlBuilder"]
  GameServerProtocolConfig["GameServerProtocolConfig"]
  GameServersManifestProvider["GameServersManifestProvider"]
  GameServersOptions["GameServersOptions"]
  GameServersPollingService["GameServersPollingService"]
  HostOverridingGameServerPollingRepository["HostOverridingGameServerPollingRepository"]
  HostOverridingGameServerTargetRepository["HostOverridingGameServerTargetRepository"]
  HumanitzProvider["HumanitzProvider"]
  HumanitzRconClient["HumanitzRconClient"]
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
  GameServerActionDefinition --> GameServerActionParameter
  GameServerDashboardController --> GetGameServerDashboardUseCase
  GameServerDetailsView --> GameServerActionDefinition
  GameServerImageUrlBuilder -.-> IGameServerImageUrlBuilder
  GameServerLiveView --> GameServerLivePlayer
  GameServerLiveView --> GameServerLiveStructure
  GameServersController --> GetGameServersUseCase
  GameServersManifestProvider -.-> IGameServersManifestProvider
  GameServersSyncController --> GameServersSyncUseCase
  GameServersSyncUseCase --> IGameServerImageUrlBuilder
  GameServersSyncUseCase --> IGameServerRepository
  GameServersSyncUseCase --> IGameServersManifestProvider
  GameServersSyncUseCase --> ISteamAppDetailsProvider
  GetGameServerDashboardUseCase --> IGameServerProvider
  GetGameServerDashboardUseCase --> IGameServerTargetRepository
  GetGameServersUseCase --> IGameServerDashboardRepository
  GetGameServersUseCase --> IGameServerProvider
  HostOverridingGameServerPollingRepository --> IGameServerPollingRepository
  HostOverridingGameServerTargetRepository --> IGameServerTargetRepository
  HumanitzProvider --> HumanitzRconClient
  HumanitzProvider -.-> IGameServerProvider
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
