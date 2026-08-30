# Temtem / Types

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  TemtemTypesController["TemtemTypesController"]
  end
  subgraph Application
  ITemtemTypeRepository(["ITemtemTypeRepository"])
  ListTemtemTypesUseCase["ListTemtemTypesUseCase"]
  TemtemTypeView["TemtemTypeView"]
  end
  subgraph Domain
  TypeEffectiveness["TypeEffectiveness"]
  end
  subgraph Infrastructure
  PostgresTemtemTypeRepository["PostgresTemtemTypeRepository"]
  end
  ListTemtemTypesUseCase --> ITemtemTypeRepository
  PostgresTemtemTypeRepository -.-> ITemtemTypeRepository
  TemtemTypesController --> ListTemtemTypesUseCase
```
