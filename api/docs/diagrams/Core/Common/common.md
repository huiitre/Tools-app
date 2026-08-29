# Core / Common

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  ApiExceptionHandler["ApiExceptionHandler"]
  ApiProblemDetailsFactory["ApiProblemDetailsFactory"]
  InternalApiAttribute["InternalApiAttribute"]
  RequestIdMiddleware["RequestIdMiddleware"]
  end
  subgraph Application
  AppException["AppException"]
  ErrorKind["ErrorKind"]
  ITransaction(["ITransaction"])
  ITransactionManager(["ITransactionManager"])
  end
  subgraph Infrastructure
  AppOptions["AppOptions"]
  PostgresConnectionString["PostgresConnectionString"]
  PostgresSession["PostgresSession"]
  PostgresTransactionManager["PostgresTransactionManager"]
  end
  subgraph Autre
  CommonModule["CommonModule"]
  end
  ApiExceptionHandler --> ApiProblemDetailsFactory
  PostgresTransactionManager --> PostgresSession
  PostgresTransactionManager -.-> ITransactionManager
```
