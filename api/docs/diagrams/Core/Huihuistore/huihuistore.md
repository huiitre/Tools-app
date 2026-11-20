# Core / Huihuistore

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  HuihuistoreController["HuihuistoreController"]
  end
  subgraph Application
  GetHuihuistoreUpdateUseCase["GetHuihuistoreUpdateUseCase"]
  HuihuistorePublishRules["HuihuistorePublishRules"]
  HuihuistoreUpdateView["HuihuistoreUpdateView"]
  IHuihuistoreReleaseStore(["IHuihuistoreReleaseStore"])
  PublishHuihuistoreReleaseUseCase["PublishHuihuistoreReleaseUseCase"]
  end
  subgraph Domain
  HuihuistoreRelease["HuihuistoreRelease"]
  end
  subgraph Infrastructure
  FileSystemHuihuistoreReleaseStore["FileSystemHuihuistoreReleaseStore"]
  HuihuistoreOptions["HuihuistoreOptions"]
  end
  subgraph Autre
  HuihuistoreModule["HuihuistoreModule"]
  end
  FileSystemHuihuistoreReleaseStore --> HuihuistoreOptions
  FileSystemHuihuistoreReleaseStore -.-> IHuihuistoreReleaseStore
  FileSystemHuihuistoreReleaseStore --> HuihuistoreRelease
  GetHuihuistoreUpdateUseCase --> IHuihuistoreReleaseStore
  GetHuihuistoreUpdateUseCase --> HuihuistoreRelease
  HuihuistoreController --> GetHuihuistoreUpdateUseCase
  HuihuistoreController --> PublishHuihuistoreReleaseUseCase
  HuihuistorePublishRules --> HuihuistoreRelease
  IHuihuistoreReleaseStore --> HuihuistoreRelease
  PublishHuihuistoreReleaseUseCase --> HuihuistoreOptions
  PublishHuihuistoreReleaseUseCase --> IHuihuistoreReleaseStore
  PublishHuihuistoreReleaseUseCase --> HuihuistoreRelease
```
