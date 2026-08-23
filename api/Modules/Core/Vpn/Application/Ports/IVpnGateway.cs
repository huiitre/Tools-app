using Tools.Api.Modules.Core.Vpn.Application.Dto;

namespace Tools.Api.Modules.Core.Vpn.Application.Ports;

public interface IVpnGateway
{
    Task<IReadOnlyList<VpnPeerDto>> FindPeersAsync();
    Task<VpnPeerDto> AddPeerAsync(string name);
    Task RemovePeerAsync(string name);
}