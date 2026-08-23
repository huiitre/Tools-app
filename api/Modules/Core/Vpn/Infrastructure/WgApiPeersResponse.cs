using Tools.Api.Modules.Core.Vpn.Application.Dto;

namespace Tools.Api.Modules.Core.Vpn.Infrastructure;

// Enveloppe de la réponse du service WireGuard : { "peers": [...] }.
public sealed record WgApiPeersResponse(List<VpnPeerDto> Peers);
