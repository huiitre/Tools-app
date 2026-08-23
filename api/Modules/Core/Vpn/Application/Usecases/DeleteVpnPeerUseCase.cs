using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Vpn.Application.Dto;
using Tools.Api.Modules.Core.Vpn.Application.Ports;

namespace Tools.Api.Modules.Core.Vpn.Application.Usecases;

public sealed class DeleteVpnPeerUseCase(
    UseCaseAuthorizer authorizer,
    IVpnGateway vpnGateway
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public Task Execute(string name)
    {
        return vpnGateway.RemovePeerAsync(name);
    }
}