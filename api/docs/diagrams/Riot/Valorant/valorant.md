# Riot / Valorant

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  ValorantAuthController["ValorantAuthController"]
  ValorantBundleController["ValorantBundleController"]
  ValorantSkinController["ValorantSkinController"]
  ValorantStoreController["ValorantStoreController"]
  ValorantStoreHistoryController["ValorantStoreHistoryController"]
  ValorantUserSkinController["ValorantUserSkinController"]
  ValorantVersionController["ValorantVersionController"]
  ValorantWatchlistController["ValorantWatchlistController"]
  ValorantWeaponController["ValorantWeaponController"]
  end
  subgraph Application
  AddMyValorantSkinUseCase["AddMyValorantSkinUseCase"]
  AddSkinToStoreHistoryCommand["AddSkinToStoreHistoryCommand"]
  AddSkinToStoreHistoryUseCase["AddSkinToStoreHistoryUseCase"]
  AddSkinToWatchlistUseCase["AddSkinToWatchlistUseCase"]
  AddToWatchlistCommand["AddToWatchlistCommand"]
  AddUserSkinCommand["AddUserSkinCommand"]
  GetMyValorantStoreHistoryUseCase["GetMyValorantStoreHistoryUseCase"]
  GetMyValorantUserSkinsUseCase["GetMyValorantUserSkinsUseCase"]
  GetMyValorantWatchlistUseCase["GetMyValorantWatchlistUseCase"]
  GetValorantAccessTokenUseCase["GetValorantAccessTokenUseCase"]
  GetValorantBundleByAssetIdUseCase["GetValorantBundleByAssetIdUseCase"]
  GetValorantBundleUseCase["GetValorantBundleUseCase"]
  GetValorantSkinByAssetIdUseCase["GetValorantSkinByAssetIdUseCase"]
  GetValorantSkinByLevelUseCase["GetValorantSkinByLevelUseCase"]
  GetValorantSkinUseCase["GetValorantSkinUseCase"]
  GetValorantStoreUseCase["GetValorantStoreUseCase"]
  GetValorantVersionUseCase["GetValorantVersionUseCase"]
  GetValorantWeaponSkinsUseCase["GetValorantWeaponSkinsUseCase"]
  GetValorantWeaponUseCase["GetValorantWeaponUseCase"]
  IRiotAuthPort(["IRiotAuthPort"])
  IValorantAuthRepository(["IValorantAuthRepository"])
  IValorantBundleRepository(["IValorantBundleRepository"])
  IValorantSkinRepository(["IValorantSkinRepository"])
  IValorantStoreHistoryRepository(["IValorantStoreHistoryRepository"])
  IValorantStorePort(["IValorantStorePort"])
  IValorantTokenCipher(["IValorantTokenCipher"])
  IValorantTokenParser(["IValorantTokenParser"])
  IValorantUserSkinRepository(["IValorantUserSkinRepository"])
  IValorantVersionProvider(["IValorantVersionProvider"])
  IValorantWatchlistRepository(["IValorantWatchlistRepository"])
  IValorantWeaponRepository(["IValorantWeaponRepository"])
  LinkValorantAccountCommand["LinkValorantAccountCommand"]
  LinkValorantAccountUseCase["LinkValorantAccountUseCase"]
  ListValorantAccountsUseCase["ListValorantAccountsUseCase"]
  ListValorantBundlesUseCase["ListValorantBundlesUseCase"]
  ListValorantSkinsByThemeUseCase["ListValorantSkinsByThemeUseCase"]
  ListValorantSkinsUseCase["ListValorantSkinsUseCase"]
  ListValorantWeaponsUseCase["ListValorantWeaponsUseCase"]
  RemoveMyValorantSkinUseCase["RemoveMyValorantSkinUseCase"]
  RemoveSkinFromWatchlistUseCase["RemoveSkinFromWatchlistUseCase"]
  RenameValorantAccountCommand["RenameValorantAccountCommand"]
  RenameValorantAccountUseCase["RenameValorantAccountUseCase"]
  TriggerValorantWatchlistSyncUseCase["TriggerValorantWatchlistSyncUseCase"]
  UnlinkValorantAccountUseCase["UnlinkValorantAccountUseCase"]
  ValorantAccountAuthView["ValorantAccountAuthView"]
  ValorantAccountView["ValorantAccountView"]
  ValorantAuthService["ValorantAuthService"]
  ValorantBundleView["ValorantBundleView"]
  ValorantContentTierView["ValorantContentTierView"]
  ValorantNightMarket["ValorantNightMarket"]
  ValorantNightMarketOffer["ValorantNightMarketOffer"]
  ValorantSkinChromaView["ValorantSkinChromaView"]
  ValorantSkinLevelView["ValorantSkinLevelView"]
  ValorantSkinView["ValorantSkinView"]
  ValorantStoreBundle["ValorantStoreBundle"]
  ValorantStoreHistoryView["ValorantStoreHistoryView"]
  ValorantStoreOffer["ValorantStoreOffer"]
  ValorantStoreView["ValorantStoreView"]
  ValorantTokenView["ValorantTokenView"]
  ValorantWatchlistNotifier["ValorantWatchlistNotifier"]
  ValorantWeaponView["ValorantWeaponView"]
  end
  subgraph Infrastructure
  AesGcmValorantTokenCipher["AesGcmValorantTokenCipher"]
  PostgresValorantAuthRepository["PostgresValorantAuthRepository"]
  PostgresValorantBundleRepository["PostgresValorantBundleRepository"]
  PostgresValorantSkinRepository["PostgresValorantSkinRepository"]
  PostgresValorantStoreHistoryRepository["PostgresValorantStoreHistoryRepository"]
  PostgresValorantUserSkinRepository["PostgresValorantUserSkinRepository"]
  PostgresValorantWatchlistRepository["PostgresValorantWatchlistRepository"]
  PostgresValorantWeaponRepository["PostgresValorantWeaponRepository"]
  RiotAuthHttpAdapter["RiotAuthHttpAdapter"]
  ValorantAssetsVersionProvider["ValorantAssetsVersionProvider"]
  ValorantStoreHttpAdapter["ValorantStoreHttpAdapter"]
  ValorantTokenParser["ValorantTokenParser"]
  ValorantWatchlistSchedulerService["ValorantWatchlistSchedulerService"]
  end
  AddMyValorantSkinUseCase --> IValorantAuthRepository
  AddMyValorantSkinUseCase --> IValorantSkinRepository
  AddMyValorantSkinUseCase --> IValorantUserSkinRepository
  AddSkinToStoreHistoryUseCase --> IValorantAuthRepository
  AddSkinToStoreHistoryUseCase --> IValorantStoreHistoryRepository
  AddSkinToWatchlistUseCase --> IValorantAuthRepository
  AddSkinToWatchlistUseCase --> IValorantSkinRepository
  AddSkinToWatchlistUseCase --> IValorantWatchlistRepository
  AesGcmValorantTokenCipher -.-> IValorantTokenCipher
  GetMyValorantStoreHistoryUseCase --> IValorantAuthRepository
  GetMyValorantStoreHistoryUseCase --> IValorantSkinRepository
  GetMyValorantStoreHistoryUseCase --> IValorantStoreHistoryRepository
  GetMyValorantUserSkinsUseCase --> IValorantAuthRepository
  GetMyValorantUserSkinsUseCase --> IValorantSkinRepository
  GetMyValorantWatchlistUseCase --> IValorantAuthRepository
  GetMyValorantWatchlistUseCase --> IValorantSkinRepository
  GetValorantAccessTokenUseCase --> IValorantAuthRepository
  GetValorantAccessTokenUseCase --> ValorantAuthService
  GetValorantBundleByAssetIdUseCase --> IValorantBundleRepository
  GetValorantBundleUseCase --> IValorantBundleRepository
  GetValorantSkinByAssetIdUseCase --> IValorantAuthRepository
  GetValorantSkinByAssetIdUseCase --> IValorantSkinRepository
  GetValorantSkinByLevelUseCase --> IValorantAuthRepository
  GetValorantSkinByLevelUseCase --> IValorantSkinRepository
  GetValorantSkinUseCase --> IValorantAuthRepository
  GetValorantSkinUseCase --> IValorantSkinRepository
  GetValorantStoreUseCase --> IValorantAuthRepository
  GetValorantStoreUseCase --> IValorantBundleRepository
  GetValorantStoreUseCase --> IValorantSkinRepository
  GetValorantStoreUseCase --> IValorantStorePort
  GetValorantStoreUseCase --> IValorantTokenParser
  GetValorantStoreUseCase --> IValorantVersionProvider
  GetValorantStoreUseCase --> ValorantAuthService
  GetValorantVersionUseCase --> IValorantVersionProvider
  GetValorantWeaponSkinsUseCase --> IValorantAuthRepository
  GetValorantWeaponSkinsUseCase --> IValorantSkinRepository
  GetValorantWeaponSkinsUseCase --> IValorantWeaponRepository
  GetValorantWeaponUseCase --> IValorantWeaponRepository
  LinkValorantAccountUseCase --> IRiotAuthPort
  LinkValorantAccountUseCase --> IValorantStorePort
  LinkValorantAccountUseCase --> IValorantVersionProvider
  LinkValorantAccountUseCase --> ValorantAuthService
  ListValorantAccountsUseCase --> IValorantAuthRepository
  ListValorantBundlesUseCase --> IValorantBundleRepository
  ListValorantSkinsByThemeUseCase --> IValorantAuthRepository
  ListValorantSkinsByThemeUseCase --> IValorantSkinRepository
  ListValorantSkinsUseCase --> IValorantAuthRepository
  ListValorantSkinsUseCase --> IValorantSkinRepository
  ListValorantWeaponsUseCase --> IValorantWeaponRepository
  PostgresValorantAuthRepository -.-> IValorantAuthRepository
  PostgresValorantBundleRepository -.-> IValorantBundleRepository
  PostgresValorantSkinRepository -.-> IValorantSkinRepository
  PostgresValorantStoreHistoryRepository -.-> IValorantStoreHistoryRepository
  PostgresValorantUserSkinRepository -.-> IValorantUserSkinRepository
  PostgresValorantWatchlistRepository -.-> IValorantWatchlistRepository
  PostgresValorantWeaponRepository -.-> IValorantWeaponRepository
  RemoveMyValorantSkinUseCase --> IValorantAuthRepository
  RemoveMyValorantSkinUseCase --> IValorantUserSkinRepository
  RemoveSkinFromWatchlistUseCase --> IValorantAuthRepository
  RemoveSkinFromWatchlistUseCase --> IValorantWatchlistRepository
  RenameValorantAccountUseCase --> IValorantAuthRepository
  RiotAuthHttpAdapter --> IValorantTokenParser
  RiotAuthHttpAdapter -.-> IRiotAuthPort
  TriggerValorantWatchlistSyncUseCase --> ValorantWatchlistNotifier
  UnlinkValorantAccountUseCase --> IValorantAuthRepository
  ValorantAccountAuthView --> ValorantAccountView
  ValorantAssetsVersionProvider -.-> IValorantVersionProvider
  ValorantAuthController --> GetValorantAccessTokenUseCase
  ValorantAuthController --> LinkValorantAccountUseCase
  ValorantAuthController --> ListValorantAccountsUseCase
  ValorantAuthController --> RenameValorantAccountUseCase
  ValorantAuthController --> UnlinkValorantAccountUseCase
  ValorantAuthService --> IRiotAuthPort
  ValorantAuthService --> IValorantAuthRepository
  ValorantAuthService --> IValorantTokenCipher
  ValorantBundleController --> GetValorantBundleByAssetIdUseCase
  ValorantBundleController --> GetValorantBundleUseCase
  ValorantBundleController --> ListValorantBundlesUseCase
  ValorantNightMarketOffer --> ValorantSkinView
  ValorantSkinController --> GetValorantSkinByAssetIdUseCase
  ValorantSkinController --> GetValorantSkinByLevelUseCase
  ValorantSkinController --> GetValorantSkinUseCase
  ValorantSkinController --> ListValorantSkinsByThemeUseCase
  ValorantSkinController --> ListValorantSkinsUseCase
  ValorantStoreController --> GetValorantStoreUseCase
  ValorantStoreHistoryController --> AddSkinToStoreHistoryUseCase
  ValorantStoreHistoryController --> GetMyValorantStoreHistoryUseCase
  ValorantStoreHttpAdapter -.-> IValorantStorePort
  ValorantStoreOffer --> ValorantSkinView
  ValorantTokenParser -.-> IValorantTokenParser
  ValorantUserSkinController --> AddMyValorantSkinUseCase
  ValorantUserSkinController --> GetMyValorantUserSkinsUseCase
  ValorantUserSkinController --> RemoveMyValorantSkinUseCase
  ValorantVersionController --> GetValorantVersionUseCase
  ValorantWatchlistController --> AddSkinToWatchlistUseCase
  ValorantWatchlistController --> GetMyValorantWatchlistUseCase
  ValorantWatchlistController --> RemoveSkinFromWatchlistUseCase
  ValorantWatchlistController --> TriggerValorantWatchlistSyncUseCase
  ValorantWatchlistNotifier --> IValorantAuthRepository
  ValorantWatchlistNotifier --> IValorantSkinRepository
  ValorantWatchlistNotifier --> IValorantStoreHistoryRepository
  ValorantWatchlistNotifier --> IValorantStorePort
  ValorantWatchlistNotifier --> IValorantVersionProvider
  ValorantWatchlistNotifier --> ValorantAuthService
  ValorantWeaponController --> GetValorantWeaponSkinsUseCase
  ValorantWeaponController --> GetValorantWeaponUseCase
  ValorantWeaponController --> ListValorantWeaponsUseCase
```
