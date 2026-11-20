# Core / AndroidApps

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  AndroidAppsAdminController["AndroidAppsAdminController"]
  AndroidAppsCatalogueController["AndroidAppsCatalogueController"]
  end
  subgraph Application
  AnalyzeAndroidAppImportUseCase["AnalyzeAndroidAppImportUseCase"]
  AndroidAppCatalogueAppView["AndroidAppCatalogueAppView"]
  AndroidAppCatalogueReleaseView["AndroidAppCatalogueReleaseView"]
  AndroidAppCatalogueView["AndroidAppCatalogueView"]
  AndroidAppImportAnalysisView["AndroidAppImportAnalysisView"]
  AndroidAppImportExistingAppView["AndroidAppImportExistingAppView"]
  AndroidAppImportRules["AndroidAppImportRules"]
  AndroidAppImportWarningView["AndroidAppImportWarningView"]
  AndroidAppReleaseView["AndroidAppReleaseView"]
  AndroidAppView["AndroidAppView"]
  ApkInfoView["ApkInfoView"]
  CancelAndroidAppImportUseCase["CancelAndroidAppImportUseCase"]
  ConfirmAndroidAppImportCommand["ConfirmAndroidAppImportCommand"]
  ConfirmAndroidAppImportUseCase["ConfirmAndroidAppImportUseCase"]
  DeleteAndroidAppReleaseUseCase["DeleteAndroidAppReleaseUseCase"]
  DeleteAndroidAppUseCase["DeleteAndroidAppUseCase"]
  GetAndroidAppCatalogueUseCase["GetAndroidAppCatalogueUseCase"]
  Hex["Hex"]
  IAndroidAppAssetUrlBuilder(["IAndroidAppAssetUrlBuilder"])
  IAndroidAppFileStore(["IAndroidAppFileStore"])
  IAndroidAppRepository(["IAndroidAppRepository"])
  IApkInspector(["IApkInspector"])
  ListAndroidAppsUseCase["ListAndroidAppsUseCase"]
  UpdateAndroidAppCommand["UpdateAndroidAppCommand"]
  UpdateAndroidAppUseCase["UpdateAndroidAppUseCase"]
  end
  subgraph Domain
  AndroidApp["AndroidApp"]
  AndroidAppRelease["AndroidAppRelease"]
  ApkInfo["ApkInfo"]
  end
  subgraph Infrastructure
  AndroidAppAssetUrlBuilder["AndroidAppAssetUrlBuilder"]
  AndroidAppsOptions["AndroidAppsOptions"]
  AndroidBuildToolsApkInspector["AndroidBuildToolsApkInspector"]
  ApkToolOutputParser["ApkToolOutputParser"]
  FileSystemAndroidAppFileStore["FileSystemAndroidAppFileStore"]
  PostgresAndroidAppRepository["PostgresAndroidAppRepository"]
  end
  subgraph Autre
  AndroidAppsModule["AndroidAppsModule"]
  end
  AnalyzeAndroidAppImportUseCase --> IAndroidAppFileStore
  AnalyzeAndroidAppImportUseCase --> IAndroidAppRepository
  AnalyzeAndroidAppImportUseCase --> IApkInspector
  AnalyzeAndroidAppImportUseCase --> AndroidApp
  AndroidApp --> AndroidAppRelease
  AndroidAppAssetUrlBuilder -.-> IAndroidAppAssetUrlBuilder
  AndroidAppCatalogueAppView --> AndroidAppCatalogueReleaseView
  AndroidAppCatalogueView --> AndroidAppCatalogueAppView
  AndroidAppImportAnalysisView --> AndroidAppImportWarningView
  AndroidAppImportAnalysisView --> ApkInfoView
  AndroidAppImportRules --> AndroidApp
  AndroidAppImportRules --> ApkInfo
  AndroidAppReleaseView --> AndroidAppRelease
  AndroidAppView --> AndroidAppReleaseView
  AndroidAppView --> AndroidApp
  AndroidAppsAdminController --> AnalyzeAndroidAppImportUseCase
  AndroidAppsAdminController --> CancelAndroidAppImportUseCase
  AndroidAppsAdminController --> ConfirmAndroidAppImportUseCase
  AndroidAppsAdminController --> DeleteAndroidAppReleaseUseCase
  AndroidAppsAdminController --> DeleteAndroidAppUseCase
  AndroidAppsAdminController --> ListAndroidAppsUseCase
  AndroidAppsAdminController --> UpdateAndroidAppUseCase
  AndroidAppsCatalogueController --> GetAndroidAppCatalogueUseCase
  AndroidBuildToolsApkInspector --> AndroidAppsOptions
  AndroidBuildToolsApkInspector -.-> IApkInspector
  AndroidBuildToolsApkInspector --> ApkInfo
  ApkInfoView --> ApkInfo
  CancelAndroidAppImportUseCase --> IAndroidAppFileStore
  ConfirmAndroidAppImportUseCase --> IAndroidAppAssetUrlBuilder
  ConfirmAndroidAppImportUseCase --> IAndroidAppFileStore
  ConfirmAndroidAppImportUseCase --> IAndroidAppRepository
  ConfirmAndroidAppImportUseCase --> IApkInspector
  ConfirmAndroidAppImportUseCase --> AndroidApp
  DeleteAndroidAppReleaseUseCase --> IAndroidAppFileStore
  DeleteAndroidAppReleaseUseCase --> IAndroidAppRepository
  DeleteAndroidAppUseCase --> IAndroidAppFileStore
  DeleteAndroidAppUseCase --> IAndroidAppRepository
  FileSystemAndroidAppFileStore --> AndroidAppsOptions
  FileSystemAndroidAppFileStore -.-> IAndroidAppFileStore
  GetAndroidAppCatalogueUseCase --> IAndroidAppAssetUrlBuilder
  GetAndroidAppCatalogueUseCase --> IAndroidAppRepository
  IAndroidAppRepository --> AndroidApp
  IAndroidAppRepository --> ApkInfo
  IApkInspector --> ApkInfo
  ListAndroidAppsUseCase --> IAndroidAppAssetUrlBuilder
  ListAndroidAppsUseCase --> IAndroidAppRepository
  PostgresAndroidAppRepository -.-> IAndroidAppRepository
  PostgresAndroidAppRepository --> AndroidApp
  PostgresAndroidAppRepository --> AndroidAppRelease
  PostgresAndroidAppRepository --> ApkInfo
  UpdateAndroidAppUseCase --> IAndroidAppAssetUrlBuilder
  UpdateAndroidAppUseCase --> IAndroidAppRepository
  UpdateAndroidAppUseCase --> AndroidApp
```
