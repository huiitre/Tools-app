# Core / Security

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  RolesController["RolesController"]
  end
  subgraph Application
  ApiKeyCaller["ApiKeyCaller"]
  Caller["Caller"]
  CurrentApiKey["CurrentApiKey"]
  CurrentUser["CurrentUser"]
  ICurrentUserProvider(["ICurrentUserProvider"])
  IRoleRepository(["IRoleRepository"])
  ListRolesUseCase["ListRolesUseCase"]
  RoleDto["RoleDto"]
  SecuredUseCase["SecuredUseCase"]
  UseCaseAuthorizer["UseCaseAuthorizer"]
  UserCaller["UserCaller"]
  end
  subgraph Domain
  ApiKeyScope["ApiKeyScope"]
  ApiKeyScopes["ApiKeyScopes"]
  ModuleCode["ModuleCode"]
  ModuleCodes["ModuleCodes"]
  RoleCode["RoleCode"]
  RoleCodes["RoleCodes"]
  end
  subgraph Infrastructure
  ApiKeyAuthenticationDefaults["ApiKeyAuthenticationDefaults"]
  ApiKeyClaims["ApiKeyClaims"]
  HttpCurrentUserProvider["HttpCurrentUserProvider"]
  JwtClaims["JwtClaims"]
  PostgresRoleRepository["PostgresRoleRepository"]
  end
  subgraph Autre
  ModuleAuthorizationProbe["ModuleAuthorizationProbe"]
  ModuleAuthorizationProbeResult["ModuleAuthorizationProbeResult"]
  SecurityModule["SecurityModule"]
  end
  ApiKeyCaller --> CurrentApiKey
  ApiKeyCaller -.-> Caller
  HttpCurrentUserProvider -.-> ICurrentUserProvider
  HttpCurrentUserProvider --> ApiKeyScopes
  HttpCurrentUserProvider --> ModuleCodes
  HttpCurrentUserProvider --> RoleCodes
  ListRolesUseCase --> IRoleRepository
  ListRolesUseCase --> UseCaseAuthorizer
  ListRolesUseCase -.-> SecuredUseCase
  ModuleAuthorizationProbe --> UseCaseAuthorizer
  ModuleAuthorizationProbe -.-> SecuredUseCase
  PostgresRoleRepository -.-> IRoleRepository
  RolesController --> ListRolesUseCase
  UseCaseAuthorizer --> ICurrentUserProvider
  UserCaller --> CurrentUser
  UserCaller -.-> Caller
```
