using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text;
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

    // Servi en fichier plutôt qu'en JSON : le navigateur l'enregistre tel quel, et le QR code
    // aura sa propre route le jour où il servira.
    [HttpGet("{name}/config")]
    public async Task<IActionResult> Config(
        [FromServices] GetVpnPeerConfigUseCase getVpnPeerConfigUseCase,
        [FromRoute] string name
    )
    {
        var config = await getVpnPeerConfigUseCase.Execute(name);

        return File(Encoding.UTF8.GetBytes(config), "text/plain", $"{name}.conf");
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
