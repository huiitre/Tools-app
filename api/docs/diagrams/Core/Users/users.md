# Core / Users

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  SetUserActiveRequest["SetUserActiveRequest"]
  SetUserRoleRequest["SetUserRoleRequest"]
  UsersController["UsersController"]
  end
  subgraph Application
  GetMyProfileUseCase["GetMyProfileUseCase"]
  IUserRepository(["IUserRepository"])
  ListUsersUseCase["ListUsersUseCase"]
  RoleDto["RoleDto"]
  SetUserActiveCommand["SetUserActiveCommand"]
  SetUserActiveUseCase["SetUserActiveUseCase"]
  SetUserGlobalRoleCommand["SetUserGlobalRoleCommand"]
  SetUserGlobalRoleUseCase["SetUserGlobalRoleUseCase"]
  UserAdminDto["UserAdminDto"]
  UserModuleDto["UserModuleDto"]
  UserProfileDto["UserProfileDto"]
  end
  subgraph Domain
  User["User"]
  end
  subgraph Infrastructure
  PostgresUserRepository["PostgresUserRepository"]
  end
  subgraph Autre
  UsersModule["UsersModule"]
  end
  GetMyProfileUseCase --> IUserRepository
  ListUsersUseCase --> IUserRepository
  PostgresUserRepository -.-> IUserRepository
  SetUserActiveUseCase --> IUserRepository
  SetUserGlobalRoleUseCase --> IUserRepository
  UserModuleDto --> RoleDto
  UsersController --> GetMyProfileUseCase
  UsersController --> ListUsersUseCase
  UsersController --> SetUserActiveUseCase
  UsersController --> SetUserGlobalRoleUseCase
```
