# Core / ApiKeys

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  ApiKeysController["ApiKeysController"]
  end
  subgraph Application
  ApiKeyAuthenticator["ApiKeyAuthenticator"]
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
  ApiKeySecret["ApiKeySecret"]
  end
  subgraph Infrastructure
  ApiKeyAuthenticationHandler["ApiKeyAuthenticationHandler"]
  PostgresApiKeyRepository["PostgresApiKeyRepository"]
  end
  subgraph Autre
  ApiKeysModule["ApiKeysModule"]
  end
  ApiKeyAuthenticationHandler --> ApiKeyAuthenticator
  ApiKeyAuthenticationHandler --> ApiKey
  ApiKeyAuthenticator --> IApiKeyRepository
  ApiKeyAuthenticator --> ApiKey
  ApiKeyAuthenticator --> ApiKeySecret
  ApiKeyView --> ApiKey
  ApiKeysController --> CreateApiKeyUseCase
  ApiKeysController --> ListApiKeyScopesUseCase
  ApiKeysController --> ListApiKeysUseCase
  ApiKeysController --> RevokeApiKeyUseCase
  CreateApiKeyUseCase --> IApiKeyRepository
  CreateApiKeyUseCase --> ApiKey
  CreateApiKeyUseCase --> ApiKeySecret
  CreatedApiKeyView --> ApiKeyView
  CreatedApiKeyView --> ApiKey
  IApiKeyRepository --> ApiKey
  ListApiKeysUseCase --> IApiKeyRepository
  PostgresApiKeyRepository -.-> IApiKeyRepository
  PostgresApiKeyRepository --> ApiKey
  RevokeApiKeyUseCase --> IApiKeyRepository
```
