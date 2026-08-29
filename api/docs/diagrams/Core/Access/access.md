# Core / Access

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  ChangeModuleRoleRequest["ChangeModuleRoleRequest"]
  CreateModuleRequest["CreateModuleRequest"]
  ModulesController["ModulesController"]
  UpdateModuleRequest["UpdateModuleRequest"]
  end
  subgraph Application
  ChangeModuleRoleCommand["ChangeModuleRoleCommand"]
  ChangeModuleRoleUseCase["ChangeModuleRoleUseCase"]
  CreateModuleCommand["CreateModuleCommand"]
  CreateModuleUseCase["CreateModuleUseCase"]
  GrantModuleAccessCommand["GrantModuleAccessCommand"]
  GrantModuleAccessUseCase["GrantModuleAccessUseCase"]
  IModuleMembershipRepository(["IModuleMembershipRepository"])
  IModuleRepository(["IModuleRepository"])
  ListModuleMembersUseCase["ListModuleMembersUseCase"]
  ListModulesUseCase["ListModulesUseCase"]
  ModuleDto["ModuleDto"]
  ModuleMemberDto["ModuleMemberDto"]
  RevokeModuleAccessCommand["RevokeModuleAccessCommand"]
  RevokeModuleAccessUseCase["RevokeModuleAccessUseCase"]
  UpdateModuleCommand["UpdateModuleCommand"]
  UpdateModuleUseCase["UpdateModuleUseCase"]
  end
  subgraph Infrastructure
  PostgresModuleMembershipRepository["PostgresModuleMembershipRepository"]
  PostgresModuleRepository["PostgresModuleRepository"]
  end
  subgraph Autre
  AccessModule["AccessModule"]
  end
  ChangeModuleRoleUseCase --> IModuleMembershipRepository
  CreateModuleUseCase --> IModuleRepository
  GrantModuleAccessUseCase --> IModuleMembershipRepository
  GrantModuleAccessUseCase --> IModuleRepository
  ListModuleMembersUseCase --> IModuleMembershipRepository
  ListModuleMembersUseCase --> IModuleRepository
  ListModulesUseCase --> IModuleRepository
  ModulesController --> ChangeModuleRoleUseCase
  ModulesController --> CreateModuleUseCase
  ModulesController --> GrantModuleAccessUseCase
  ModulesController --> ListModuleMembersUseCase
  ModulesController --> ListModulesUseCase
  ModulesController --> RevokeModuleAccessUseCase
  ModulesController --> UpdateModuleUseCase
  PostgresModuleMembershipRepository -.-> IModuleMembershipRepository
  PostgresModuleRepository -.-> IModuleRepository
  RevokeModuleAccessUseCase --> IModuleMembershipRepository
  UpdateModuleUseCase --> IModuleRepository
```
