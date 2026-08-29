# Core / Health

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  HealthController["HealthController"]
  end
  subgraph Application
  CheckReadinessUseCase["CheckReadinessUseCase"]
  IHealthRepository(["IHealthRepository"])
  end
  subgraph Infrastructure
  PostgresHealthRepository["PostgresHealthRepository"]
  end
  subgraph Autre
  HealthModule["HealthModule"]
  end
  CheckReadinessUseCase --> IHealthRepository
  HealthController --> CheckReadinessUseCase
  PostgresHealthRepository -.-> IHealthRepository
```
