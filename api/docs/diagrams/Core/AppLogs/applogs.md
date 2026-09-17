# Core / AppLogs

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Application
  AppLogCommand["AppLogCommand"]
  AppLogContext["AppLogContext"]
  AppLogEntry["AppLogEntry"]
  AppLogService["AppLogService"]
  IAppLogContextProvider(["IAppLogContextProvider"])
  IAppLogRepository(["IAppLogRepository"])
  end
  subgraph Infrastructure
  HttpAppLogContextProvider["HttpAppLogContextProvider"]
  PostgresAppLogRepository["PostgresAppLogRepository"]
  end
  subgraph Autre
  AppLogsModule["AppLogsModule"]
  end
  AppLogService --> IAppLogContextProvider
  AppLogService --> IAppLogRepository
  HttpAppLogContextProvider -.-> IAppLogContextProvider
  PostgresAppLogRepository -.-> IAppLogRepository
```
