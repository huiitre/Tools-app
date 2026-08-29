# Core / Vpn

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  CreateVpnPeerRequest["CreateVpnPeerRequest"]
  VpnController["VpnController"]
  end
  subgraph Application
  CreateVpnPeerUseCase["CreateVpnPeerUseCase"]
  DeleteVpnPeerUseCase["DeleteVpnPeerUseCase"]
  GetVpnPeerConfigUseCase["GetVpnPeerConfigUseCase"]
  IVpnGateway(["IVpnGateway"])
  ListVpnPeersUseCase["ListVpnPeersUseCase"]
  PeerChecksDto["PeerChecksDto"]
  VpnPeerDto["VpnPeerDto"]
  end
  subgraph Infrastructure
  InMemoryVpnGateway["InMemoryVpnGateway"]
  WgApiError["WgApiError"]
  WgApiPeerDetail["WgApiPeerDetail"]
  WgApiPeersResponse["WgApiPeersResponse"]
  WireGuardVpnGateway["WireGuardVpnGateway"]
  end
  subgraph Autre
  VpnModule["VpnModule"]
  end
  CreateVpnPeerUseCase --> IVpnGateway
  DeleteVpnPeerUseCase --> IVpnGateway
  GetVpnPeerConfigUseCase --> IVpnGateway
  InMemoryVpnGateway -.-> IVpnGateway
  ListVpnPeersUseCase --> IVpnGateway
  VpnController --> CreateVpnPeerUseCase
  VpnController --> DeleteVpnPeerUseCase
  VpnController --> GetVpnPeerConfigUseCase
  VpnController --> ListVpnPeersUseCase
  VpnPeerDto --> PeerChecksDto
  WireGuardVpnGateway -.-> IVpnGateway
```
