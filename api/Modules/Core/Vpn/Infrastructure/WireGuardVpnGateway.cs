using Tools.Api.Modules.Core.Vpn.Application.Ports;
using Tools.Api.Modules.Core.Vpn.Application.Dto;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tools.Api.Modules.Core.Vpn.Infrastructure;

// L'indisponibilité du service doit remonter : une liste vide serait lue comme « aucun peer »,
// ce qui est faux et alarmant sur un écran d'administration.
public sealed class WireGuardVpnGateway(
    HttpClient httpClient
) : IVpnGateway
{
    public async Task<IReadOnlyList<VpnPeerDto>> FindPeersAsync()
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<WgApiPeersResponse>("peers");

            return response?.Peers ?? throw AppException.Unavailable(
                "VPN_GATEWAY_INVALID_RESPONSE",
                "Le service WireGuard n'a retourné aucune liste de peers."
            );
        }
        catch(HttpRequestException ex)
        {
            throw AppException.Unavailable(
                "VPN_GATEWAY_UNAVAILABLE",
                $"Le service WireGuard est injoignable : {ex.Message}"
            );
        }
        catch(TaskCanceledException ex)
        {
            throw AppException.Unavailable(
                "VPN_GATEWAY_TIMEOUT",
                $"La lecture des peers VPN a expiré : {ex.Message}"
            );
        }
        catch(JsonException ex)
        {
            throw AppException.Unavailable(
                "VPN_GATEWAY_INVALID_RESPONSE",
                $"La réponse du service WireGuard est invalide : {ex.Message}"
            );
        }
    }
}
