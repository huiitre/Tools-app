# EliteDangerous / RoadToRiches

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  ExpeditionsController["ExpeditionsController"]
  ImportExpeditionRequest["ImportExpeditionRequest"]
  RenameExpeditionRequest["RenameExpeditionRequest"]
  UpdateProgressRequest["UpdateProgressRequest"]
  end
  subgraph Application
  DeleteExpeditionUseCase["DeleteExpeditionUseCase"]
  ExpeditionDetailView["ExpeditionDetailView"]
  ExpeditionSummaryView["ExpeditionSummaryView"]
  ExportExpeditionUseCase["ExportExpeditionUseCase"]
  GetExpeditionUseCase["GetExpeditionUseCase"]
  IExpeditionRepository(["IExpeditionRepository"])
  IRouteImporter(["IRouteImporter"])
  ImportExpeditionCommand["ImportExpeditionCommand"]
  ImportExpeditionUseCase["ImportExpeditionUseCase"]
  ListExpeditionsUseCase["ListExpeditionsUseCase"]
  RenameExpeditionCommand["RenameExpeditionCommand"]
  RenameExpeditionUseCase["RenameExpeditionUseCase"]
  UpdateProgressCommand["UpdateProgressCommand"]
  UpdateProgressUseCase["UpdateProgressUseCase"]
  end
  subgraph Domain
  Expedition["Expedition"]
  end
  subgraph Infrastructure
  PostgresExpeditionRepository["PostgresExpeditionRepository"]
  SpanshJsonRouteImporter["SpanshJsonRouteImporter"]
  end
  DeleteExpeditionUseCase --> IExpeditionRepository
  ExpeditionsController --> DeleteExpeditionUseCase
  ExpeditionsController --> ExportExpeditionUseCase
  ExpeditionsController --> GetExpeditionUseCase
  ExpeditionsController --> ImportExpeditionUseCase
  ExpeditionsController --> ListExpeditionsUseCase
  ExpeditionsController --> RenameExpeditionUseCase
  ExpeditionsController --> UpdateProgressUseCase
  ExportExpeditionUseCase --> IExpeditionRepository
  GetExpeditionUseCase --> IExpeditionRepository
  IExpeditionRepository --> Expedition
  ImportExpeditionUseCase --> IExpeditionRepository
  ImportExpeditionUseCase --> IRouteImporter
  ImportExpeditionUseCase --> Expedition
  ListExpeditionsUseCase --> IExpeditionRepository
  PostgresExpeditionRepository -.-> IExpeditionRepository
  PostgresExpeditionRepository --> Expedition
  RenameExpeditionUseCase --> IExpeditionRepository
  SpanshJsonRouteImporter -.-> IRouteImporter
  UpdateProgressUseCase --> IExpeditionRepository
```
