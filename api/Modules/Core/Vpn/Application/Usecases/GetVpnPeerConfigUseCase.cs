using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Vpn.Application.Ports;

namespace Tools.Api.Modules.Core.Vpn.Application.Usecases;

// La configuration porte la clé privée du client : quiconque l'obtient devient ce peer.
public sealed class GetVpnPeerConfigUseCase(
    UseCaseAuthorizer authorizer,
    IVpnGateway vpnGateway
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public Task<string> Execute(string name)
    {
        return vpnGateway.FindPeerConfigAsync(name);
    }
}
