application
├── common
├── core
│
├── dofus
│   ├── gameversion
│   │   ├── GameVersionData.java
│   │   └── GameVersionRepository.java
│   │
│   ├── itemtype
│   │   ├── ListItemTypeUseCase.java
│   │   └── ItemTypeListData.java
│   │
│   ├── ports
│   │   ├── repositories
│   │   │   └── ItemTypeRepository.java
│   │   │
│   │   └── providers
│   │       ├── ItemTypeDataProvider.java
│   │       ├── ItemDataProvider.java
│   │       └── LanguageDataProvider.java
│   │
│   └── sync
│       ├── SyncDofusDataUseCase.java
│       ├── SyncDofus3DataUseCase.java
│       │
│       ├── itemtype
│       │   ├── SyncItemTypesUseCase.java
│       │   ├── SyncItemTypesDataUseCase.java
│       │   └── ItemTypeSyncData.java
│       │
│       └── item
│           └── ItemSyncData.java
│
├── health
├── todolist
└── test
