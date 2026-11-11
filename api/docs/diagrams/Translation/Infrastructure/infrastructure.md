# Translation / Infrastructure

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Infrastructure
  LibreTranslateOptions["LibreTranslateOptions"]
  LibreTranslateProvider["LibreTranslateProvider"]
  end
  LibreTranslateProvider --> LibreTranslateOptions
```
