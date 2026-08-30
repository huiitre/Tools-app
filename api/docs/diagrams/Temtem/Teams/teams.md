# Temtem / Teams

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  TemtemTeamsController["TemtemTeamsController"]
  end
  subgraph Application
  AddTemtemTeamMemberCommand["AddTemtemTeamMemberCommand"]
  AddTemtemTeamMemberUseCase["AddTemtemTeamMemberUseCase"]
  CreateTemtemTeamCommand["CreateTemtemTeamCommand"]
  CreateTemtemTeamUseCase["CreateTemtemTeamUseCase"]
  DeleteTemtemTeamUseCase["DeleteTemtemTeamUseCase"]
  ITemtemTeamRepository(["ITemtemTeamRepository"])
  ListMyTemtemTeamsUseCase["ListMyTemtemTeamsUseCase"]
  RemoveTemtemTeamMemberUseCase["RemoveTemtemTeamMemberUseCase"]
  RenameTemtemTeamCommand["RenameTemtemTeamCommand"]
  RenameTemtemTeamUseCase["RenameTemtemTeamUseCase"]
  SetTemtemTeamMemberTechniquesCommand["SetTemtemTeamMemberTechniquesCommand"]
  SetTemtemTeamMemberTechniquesUseCase["SetTemtemTeamMemberTechniquesUseCase"]
  TemtemTeamMemberView["TemtemTeamMemberView"]
  TemtemTeamName["TemtemTeamName"]
  TemtemTeamView["TemtemTeamView"]
  end
  subgraph Domain
  TeamRoster["TeamRoster"]
  end
  subgraph Infrastructure
  PostgresTemtemTeamRepository["PostgresTemtemTeamRepository"]
  end
  AddTemtemTeamMemberUseCase --> ITemtemTeamRepository
  AddTemtemTeamMemberUseCase --> TeamRoster
  CreateTemtemTeamUseCase --> ITemtemTeamRepository
  DeleteTemtemTeamUseCase --> ITemtemTeamRepository
  ListMyTemtemTeamsUseCase --> ITemtemTeamRepository
  PostgresTemtemTeamRepository -.-> ITemtemTeamRepository
  RemoveTemtemTeamMemberUseCase --> ITemtemTeamRepository
  RenameTemtemTeamUseCase --> ITemtemTeamRepository
  SetTemtemTeamMemberTechniquesUseCase --> ITemtemTeamRepository
  SetTemtemTeamMemberTechniquesUseCase --> TeamRoster
  TemtemTeamView --> TemtemTeamMemberView
  TemtemTeamsController --> AddTemtemTeamMemberUseCase
  TemtemTeamsController --> CreateTemtemTeamUseCase
  TemtemTeamsController --> DeleteTemtemTeamUseCase
  TemtemTeamsController --> ListMyTemtemTeamsUseCase
  TemtemTeamsController --> RemoveTemtemTeamMemberUseCase
  TemtemTeamsController --> RenameTemtemTeamUseCase
  TemtemTeamsController --> SetTemtemTeamMemberTechniquesUseCase
```
