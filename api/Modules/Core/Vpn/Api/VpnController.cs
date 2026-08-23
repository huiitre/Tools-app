using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.Vpn.Application.Dto;
using Tools.Api.Modules.Core.Vpn.Application.Usecases;

namespace Tools.Api.Modules.Core.Vpn.Api;

[ApiController]
[Route("vpn")]
public class VpnController : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<VpnPeerDto>> List(
        [FromServices] ListVpnPeersUseCase listVpnPeersUseCase
    )
    {
        return listVpnPeersUseCase.Execute();
    }
}