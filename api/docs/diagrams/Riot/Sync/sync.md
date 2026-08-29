# Riot / Sync

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  ValorantSyncController["ValorantSyncController"]
  end
  subgraph Application
  IValorantBundleDataProvider(["IValorantBundleDataProvider"])
  IValorantBundleSyncRepository(["IValorantBundleSyncRepository"])
  IValorantContentTierDataProvider(["IValorantContentTierDataProvider"])
  IValorantContentTierSyncRepository(["IValorantContentTierSyncRepository"])
  IValorantSkinChromaSyncRepository(["IValorantSkinChromaSyncRepository"])
  IValorantSkinDataProvider(["IValorantSkinDataProvider"])
  IValorantSkinLevelSyncRepository(["IValorantSkinLevelSyncRepository"])
  IValorantSkinSyncRepository(["IValorantSkinSyncRepository"])
  IValorantWeaponDataProvider(["IValorantWeaponDataProvider"])
  IValorantWeaponSyncRepository(["IValorantWeaponSyncRepository"])
  SyncValorantBundlesUseCase["SyncValorantBundlesUseCase"]
  SyncValorantContentTiersUseCase["SyncValorantContentTiersUseCase"]
  SyncValorantSkinsUseCase["SyncValorantSkinsUseCase"]
  SyncValorantUseCase["SyncValorantUseCase"]
  SyncValorantWeaponsUseCase["SyncValorantWeaponsUseCase"]
  ValorantBundleSyncData["ValorantBundleSyncData"]
  ValorantContentTierSyncData["ValorantContentTierSyncData"]
  ValorantGlobalSyncReport["ValorantGlobalSyncReport"]
  ValorantSkinChromaSyncData["ValorantSkinChromaSyncData"]
  ValorantSkinLevelSyncData["ValorantSkinLevelSyncData"]
  ValorantSkinSyncData["ValorantSkinSyncData"]
  ValorantSyncReport["ValorantSyncReport"]
  ValorantWeaponSyncData["ValorantWeaponSyncData"]
  ValorantWeaponSyncResult["ValorantWeaponSyncResult"]
  end
  subgraph Infrastructure
  PostgresValorantBundleSyncRepository["PostgresValorantBundleSyncRepository"]
  PostgresValorantContentTierSyncRepository["PostgresValorantContentTierSyncRepository"]
  PostgresValorantSkinChromaSyncRepository["PostgresValorantSkinChromaSyncRepository"]
  PostgresValorantSkinLevelSyncRepository["PostgresValorantSkinLevelSyncRepository"]
  PostgresValorantSkinSyncRepository["PostgresValorantSkinSyncRepository"]
  PostgresValorantWeaponSyncRepository["PostgresValorantWeaponSyncRepository"]
  ValorantAssetsBundleDataProvider["ValorantAssetsBundleDataProvider"]
  ValorantAssetsContentTierDataProvider["ValorantAssetsContentTierDataProvider"]
  ValorantAssetsSkinDataProvider["ValorantAssetsSkinDataProvider"]
  ValorantAssetsWeaponDataProvider["ValorantAssetsWeaponDataProvider"]
  end
  PostgresValorantBundleSyncRepository -.-> IValorantBundleSyncRepository
  PostgresValorantContentTierSyncRepository -.-> IValorantContentTierSyncRepository
  PostgresValorantSkinChromaSyncRepository -.-> IValorantSkinChromaSyncRepository
  PostgresValorantSkinLevelSyncRepository -.-> IValorantSkinLevelSyncRepository
  PostgresValorantSkinSyncRepository -.-> IValorantSkinSyncRepository
  PostgresValorantWeaponSyncRepository -.-> IValorantWeaponSyncRepository
  SyncValorantBundlesUseCase --> IValorantBundleDataProvider
  SyncValorantBundlesUseCase --> IValorantBundleSyncRepository
  SyncValorantContentTiersUseCase --> IValorantContentTierDataProvider
  SyncValorantContentTiersUseCase --> IValorantContentTierSyncRepository
  SyncValorantSkinsUseCase --> IValorantSkinChromaSyncRepository
  SyncValorantSkinsUseCase --> IValorantSkinDataProvider
  SyncValorantSkinsUseCase --> IValorantSkinLevelSyncRepository
  SyncValorantSkinsUseCase --> IValorantSkinSyncRepository
  SyncValorantUseCase --> SyncValorantBundlesUseCase
  SyncValorantUseCase --> SyncValorantContentTiersUseCase
  SyncValorantUseCase --> SyncValorantSkinsUseCase
  SyncValorantUseCase --> SyncValorantWeaponsUseCase
  SyncValorantWeaponsUseCase --> IValorantWeaponDataProvider
  SyncValorantWeaponsUseCase --> IValorantWeaponSyncRepository
  ValorantAssetsBundleDataProvider -.-> IValorantBundleDataProvider
  ValorantAssetsContentTierDataProvider -.-> IValorantContentTierDataProvider
  ValorantAssetsSkinDataProvider -.-> IValorantSkinDataProvider
  ValorantAssetsWeaponDataProvider -.-> IValorantWeaponDataProvider
  ValorantGlobalSyncReport --> ValorantSyncReport
  ValorantSyncController --> SyncValorantUseCase
  ValorantWeaponSyncResult --> ValorantSyncReport
```
