# Temtem / Sync

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  TemtemSyncController["TemtemSyncController"]
  end
  subgraph Application
  ITemtemCatalogueRepository(["ITemtemCatalogueRepository"])
  ITemtemDataProvider(["ITemtemDataProvider"])
  SyncTemtemCatalogueUseCase["SyncTemtemCatalogueUseCase"]
  TemtemCatalogueSyncReport["TemtemCatalogueSyncReport"]
  TemtemCategoryData["TemtemCategoryData"]
  TemtemCreatureData["TemtemCreatureData"]
  TemtemLearningData["TemtemLearningData"]
  TemtemLinkSyncReport["TemtemLinkSyncReport"]
  TemtemPriorityData["TemtemPriorityData"]
  TemtemStatsData["TemtemStatsData"]
  TemtemSyncReport["TemtemSyncReport"]
  TemtemTechniqueData["TemtemTechniqueData"]
  TemtemTraitData["TemtemTraitData"]
  TemtemTraitLinkData["TemtemTraitLinkData"]
  TemtemTypeData["TemtemTypeData"]
  TemtemTypeMatchupData["TemtemTypeMatchupData"]
  TemtemUpsertOutcome["TemtemUpsertOutcome"]
  end
  subgraph Infrastructure
  PostgresTemtemCatalogueRepository["PostgresTemtemCatalogueRepository"]
  TemtemAssetJson["TemtemAssetJson"]
  TemtemAssetUrlBuilder["TemtemAssetUrlBuilder"]
  TemtemAssetsDataProvider["TemtemAssetsDataProvider"]
  TemtemAssetsReader["TemtemAssetsReader"]
  end
  PostgresTemtemCatalogueRepository -.-> ITemtemCatalogueRepository
  SyncTemtemCatalogueUseCase --> ITemtemCatalogueRepository
  SyncTemtemCatalogueUseCase --> ITemtemDataProvider
  TemtemAssetsDataProvider --> TemtemAssetUrlBuilder
  TemtemAssetsDataProvider --> TemtemAssetsReader
  TemtemAssetsDataProvider -.-> ITemtemDataProvider
  TemtemCatalogueSyncReport --> TemtemLinkSyncReport
  TemtemCatalogueSyncReport --> TemtemSyncReport
  TemtemCreatureData --> TemtemStatsData
  TemtemSyncController --> SyncTemtemCatalogueUseCase
```
