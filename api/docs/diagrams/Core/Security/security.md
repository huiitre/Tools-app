# Core / Security

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  RolesController["RolesController"]
  end
  subgraph Application
  CurrentUser["CurrentUser"]
  ICurrentUserProvider(["ICurrentUserProvider"])
  IRoleRepository(["IRoleRepository"])
  ListRolesUseCase["ListRolesUseCase"]
  RoleDto["RoleDto"]
  SecuredUseCase["SecuredUseCase"]
  UseCaseAuthorizer["UseCaseAuthorizer"]
  end
  subgraph Domain
  ModuleCode["ModuleCode"]
  ModuleCodes["ModuleCodes"]
  RoleCode["RoleCode"]
  RoleCodes["RoleCodes"]
  end
  subgraph Infrastructure
  HttpCurrentUserProvider["HttpCurrentUserProvider"]
  JwtClaims["JwtClaims"]
  PostgresRoleRepository["PostgresRoleRepository"]
  end
  subgraph Autre
  ModuleAuthorizationProbe["ModuleAuthorizationProbe"]
  ModuleAuthorizationProbeResult["ModuleAuthorizationProbeResult"]
  SecurityModule["SecurityModule"]
  end
  HttpCurrentUserProvider -.-> ICurrentUserProvider
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
```
