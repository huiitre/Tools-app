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
  AppLogListQuery["AppLogListQuery"]
  AppLogPageDto["AppLogPageDto"]
  AppLogService["AppLogService"]
  AppLogSortColumn["AppLogSortColumn"]
  IAppLogContextProvider(["IAppLogContextProvider"])
  IAppLogRepository(["IAppLogRepository"])
  ListAppLogsUseCase["ListAppLogsUseCase"]
  SortDirection["SortDirection"]
  end
  subgraph Infrastructure
  HttpAppLogContextProvider["HttpAppLogContextProvider"]
  PostgresAppLogRepository["PostgresAppLogRepository"]
  end
  subgraph Autre
  AppLogsModule["AppLogsModule"]
  end
  AppLogListQuery --> AppLogSortColumn
  AppLogListQuery --> SortDirection
  AppLogPageDto --> AppLogAdminDto
  AppLogService --> IAppLogContextProvider
  AppLogService --> IAppLogRepository
  AppLogsController --> ListAppLogsUseCase
  HttpAppLogContextProvider -.-> IAppLogContextProvider
  ListAppLogsUseCase --> IAppLogRepository
  PostgresAppLogRepository -.-> IAppLogRepository
```
