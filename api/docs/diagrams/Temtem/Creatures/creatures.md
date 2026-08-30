# Temtem / Creatures

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  TemtemCreaturesController["TemtemCreaturesController"]
  end
  subgraph Application
  GetTemtemBySlugUseCase["GetTemtemBySlugUseCase"]
  ITemtemCreatureRepository(["ITemtemCreatureRepository"])
  ListTemtemCreaturesUseCase["ListTemtemCreaturesUseCase"]
  TemtemDetailView["TemtemDetailView"]
  TemtemLearnedTechniqueView["TemtemLearnedTechniqueView"]
  TemtemStatsView["TemtemStatsView"]
  TemtemSummaryView["TemtemSummaryView"]
  end
  subgraph Infrastructure
  PostgresTemtemCreatureRepository["PostgresTemtemCreatureRepository"]
  TemtemCreatureSql["TemtemCreatureSql"]
  end
  GetTemtemBySlugUseCase --> ITemtemCreatureRepository
  ListTemtemCreaturesUseCase --> ITemtemCreatureRepository
  PostgresTemtemCreatureRepository -.-> ITemtemCreatureRepository
  TemtemCreaturesController --> GetTemtemBySlugUseCase
  TemtemCreaturesController --> ListTemtemCreaturesUseCase
  TemtemDetailView --> TemtemLearnedTechniqueView
  TemtemDetailView --> TemtemSummaryView
  TemtemSummaryView --> TemtemStatsView
```
