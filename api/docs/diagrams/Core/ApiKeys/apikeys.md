# Core / ApiKeys

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  ApiKeysController["ApiKeysController"]
  end
  subgraph Application
  ApiKeyView["ApiKeyView"]
  CreateApiKeyCommand["CreateApiKeyCommand"]
  CreateApiKeyUseCase["CreateApiKeyUseCase"]
  CreatedApiKeyView["CreatedApiKeyView"]
  IApiKeyRepository(["IApiKeyRepository"])
  ListApiKeyScopesUseCase["ListApiKeyScopesUseCase"]
  ListApiKeysUseCase["ListApiKeysUseCase"]
  RevokeApiKeyUseCase["RevokeApiKeyUseCase"]
  end
  subgraph Domain
  ApiKey["ApiKey"]
  ApiKeyScope["ApiKeyScope"]
  ApiKeyScopes["ApiKeyScopes"]
  ApiKeySecret["ApiKeySecret"]
  end
  subgraph Infrastructure
  PostgresApiKeyRepository["PostgresApiKeyRepository"]
  end
  subgraph Autre
  ApiKeysModule["ApiKeysModule"]
  end
  ApiKey --> ApiKeyScopes
  ApiKeyView --> ApiKey
  ApiKeysController --> CreateApiKeyUseCase
  ApiKeysController --> ListApiKeyScopesUseCase
  ApiKeysController --> ListApiKeysUseCase
  ApiKeysController --> RevokeApiKeyUseCase
  CreateApiKeyUseCase --> IApiKeyRepository
  CreateApiKeyUseCase --> ApiKey
  CreateApiKeyUseCase --> ApiKeyScopes
  CreateApiKeyUseCase --> ApiKeySecret
  CreatedApiKeyView --> ApiKeyView
  CreatedApiKeyView --> ApiKey
  IApiKeyRepository --> ApiKey
  ListApiKeyScopesUseCase --> ApiKeyScopes
  ListApiKeysUseCase --> IApiKeyRepository
  PostgresApiKeyRepository -.-> IApiKeyRepository
  PostgresApiKeyRepository --> ApiKey
  RevokeApiKeyUseCase --> IApiKeyRepository
```
