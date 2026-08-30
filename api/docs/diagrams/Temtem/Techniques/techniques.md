# Temtem / Techniques

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Application
  TemtemCategoryView["TemtemCategoryView"]
  TemtemPriorityView["TemtemPriorityView"]
  TemtemTechniqueView["TemtemTechniqueView"]
  end
  subgraph Infrastructure
  TemtemTechniqueSql["TemtemTechniqueSql"]
  end
  TemtemTechniqueView --> TemtemCategoryView
  TemtemTechniqueView --> TemtemPriorityView
```
