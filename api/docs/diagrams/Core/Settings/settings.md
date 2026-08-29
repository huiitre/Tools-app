# Core / Settings

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Application
  ISettingValueRepository(["ISettingValueRepository"])
  SettingReader["SettingReader"]
  end
  subgraph Domain
  BooleanSetting["BooleanSetting"]
  ChoiceSetting["ChoiceSetting"]
  DecimalSetting["DecimalSetting"]
  IntegerSetting["IntegerSetting"]
  MultiChoiceSetting["MultiChoiceSetting"]
  ResolvedSetting["ResolvedSetting"]
  SettingAudience["SettingAudience"]
  SettingCatalog["SettingCatalog"]
  SettingDefinition["SettingDefinition"]
  SettingResolution["SettingResolution"]
  SettingScope["SettingScope"]
  SettingScopes["SettingScopes"]
  SettingValue["SettingValue"]
  TextSetting["TextSetting"]
  end
  subgraph Infrastructure
  PostgresSettingValueRepository["PostgresSettingValueRepository"]
  end
  subgraph Autre
  SettingsModule["SettingsModule"]
  end
  BooleanSetting -.-> SettingDefinition
  ChoiceSetting -.-> SettingDefinition
  DecimalSetting -.-> SettingDefinition
  ISettingValueRepository --> SettingResolution
  ISettingValueRepository --> SettingValue
  IntegerSetting -.-> SettingDefinition
  MultiChoiceSetting -.-> SettingDefinition
  PostgresSettingValueRepository -.-> ISettingValueRepository
  PostgresSettingValueRepository --> SettingValue
  ResolvedSetting --> SettingDefinition
  SettingAudience --> SettingDefinition
  SettingCatalog --> BooleanSetting
  SettingCatalog --> ChoiceSetting
  SettingCatalog --> IntegerSetting
  SettingCatalog --> SettingDefinition
  SettingCatalog --> SettingScopes
  SettingDefinition --> SettingCatalog
  SettingReader --> ISettingValueRepository
  SettingReader --> ResolvedSetting
  SettingReader --> SettingAudience
  SettingReader --> SettingCatalog
  SettingReader --> SettingDefinition
  SettingReader --> SettingResolution
  SettingReader --> SettingValue
  SettingResolution --> ResolvedSetting
  SettingResolution --> SettingAudience
  SettingResolution --> SettingDefinition
  SettingResolution --> SettingValue
  SettingValue --> SettingScope
  SettingsModule --> SettingCatalog
  TextSetting -.-> SettingDefinition
```
