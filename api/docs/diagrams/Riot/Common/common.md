# Riot / Common

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Infrastructure
  DateOnlyTypeHandler["DateOnlyTypeHandler"]
  RiotDatabase["RiotDatabase"]
  ValorantAssetJson["ValorantAssetJson"]
  ValorantAssetUrlBuilder["ValorantAssetUrlBuilder"]
  ValorantAssetsReader["ValorantAssetsReader"]
  end
```
