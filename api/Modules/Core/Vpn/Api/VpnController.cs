using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Tools.Api.Modules.Core.Vpn.Application.Dto;
using Tools.Api.Modules.Core.Vpn.Application.Usecases;

namespace Tools.Api.Modules.Core.Vpn.Api;

[ApiController]
[Route("vpn/peers")]
public class VpnController : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<VpnPeerDto>> List(
        [FromServices] ListVpnPeersUseCase listVpnPeersUseCase
    )
    {
        return listVpnPeersUseCase.Execute();
    }

    [HttpPost]
    public Task<VpnPeerDto> Create(
        [FromServices] CreateVpnPeerUseCase createVpnPeerUseCase,
        [FromBody] CreateVpnPeerRequest request
    )
    {
        return createVpnPeerUseCase.Execute(request.Name);
    }

    [HttpDelete("{name}")]
    public Task Delete(
        [FromServices] DeleteVpnPeerUseCase deleteVpnPeerUseCase,
        [FromRoute] string name
    )
    {
        return deleteVpnPeerUseCase.Execute(name);
    }
}

public sealed record CreateVpnPeerRequest([Required] string Name);
