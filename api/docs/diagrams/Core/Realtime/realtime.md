# Core / Realtime

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  InternalRealtimePublishController["InternalRealtimePublishController"]
  PublishRealtimeEventRequest["PublishRealtimeEventRequest"]
  end
  subgraph Application
  IRealtimePublisher(["IRealtimePublisher"])
  IRecipientResolver(["IRecipientResolver"])
  PublishRealtimeEventCommand["PublishRealtimeEventCommand"]
  PublishRealtimeEventUseCase["PublishRealtimeEventUseCase"]
  RealtimeEventService["RealtimeEventService"]
  end
  subgraph Infrastructure
  CoreHub["CoreHub"]
  PostgresRecipientResolver["PostgresRecipientResolver"]
  SignalRRealtimePublisher["SignalRRealtimePublisher"]
  end
  subgraph Autre
  RealtimeModule["RealtimeModule"]
  end
  InternalRealtimePublishController --> PublishRealtimeEventUseCase
  PostgresRecipientResolver -.-> IRecipientResolver
  PublishRealtimeEventUseCase --> RealtimeEventService
  RealtimeEventService --> IRealtimePublisher
  RealtimeEventService --> IRecipientResolver
  SignalRRealtimePublisher -.-> IRealtimePublisher
```
