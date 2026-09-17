# Core / AppLogs

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  AppLogsController["AppLogsController"]
  ListAppLogsRequest["ListAppLogsRequest"]
  end
  subgraph Application
  AppLogAdminDto["AppLogAdminDto"]
  AppLogCommand["AppLogCommand"]
  AppLogContext["AppLogContext"]
  AppLogEntry["AppLogEntry"]
  AppLogFilterOptionsDto["AppLogFilterOptionsDto"]
  AppLogIpLocationDto["AppLogIpLocationDto"]
  AppLogListQuery["AppLogListQuery"]
  AppLogPageDto["AppLogPageDto"]
  AppLogService["AppLogService"]
  AppLogSortColumn["AppLogSortColumn"]
  IAppLogContextProvider(["IAppLogContextProvider"])
  IAppLogRepository(["IAppLogRepository"])
  IGeoIpLookup(["IGeoIpLookup"])
  ListAppLogsUseCase["ListAppLogsUseCase"]
  SortDirection["SortDirection"]
  end
  subgraph Infrastructure
  HttpAppLogContextProvider["HttpAppLogContextProvider"]
  MaxMindGeoIpLookup["MaxMindGeoIpLookup"]
  PostgresAppLogRepository["PostgresAppLogRepository"]
  end
  subgraph Autre
  AppLogsModule["AppLogsModule"]
  end
  AppLogListQuery --> AppLogSortColumn
  AppLogListQuery --> SortDirection
  AppLogPageDto --> AppLogAdminDto
  AppLogPageDto --> AppLogFilterOptionsDto
  AppLogService --> IAppLogContextProvider
  AppLogService --> IAppLogRepository
  AppLogsController --> ListAppLogsUseCase
  HttpAppLogContextProvider -.-> IAppLogContextProvider
  ListAppLogsUseCase --> IAppLogRepository
  ListAppLogsUseCase --> IGeoIpLookup
  MaxMindGeoIpLookup -.-> IGeoIpLookup
  PostgresAppLogRepository -.-> IAppLogRepository
```
