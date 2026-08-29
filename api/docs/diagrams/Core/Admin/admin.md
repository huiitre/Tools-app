# Core / Admin

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  AdminController["AdminController"]
  end
  subgraph Application
  AdminStatsDto["AdminStatsDto"]
  GetAdminStatsUseCase["GetAdminStatsUseCase"]
  IAdminStatsRepository(["IAdminStatsRepository"])
  ModuleUserCountDto["ModuleUserCountDto"]
  end
  subgraph Infrastructure
  PostgresAdminStatsRepository["PostgresAdminStatsRepository"]
  end
  subgraph Autre
  AdminModule["AdminModule"]
  end
  AdminController --> GetAdminStatsUseCase
  AdminStatsDto --> ModuleUserCountDto
  GetAdminStatsUseCase --> IAdminStatsRepository
  PostgresAdminStatsRepository -.-> IAdminStatsRepository
```
