using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Vpn.Application.Dto;
using Tools.Api.Modules.Core.Vpn.Application.Ports;

namespace Tools.Api.Modules.Core.Vpn.Infrastructure;

// L'indisponibilité du service doit remonter : une liste vide serait lue comme « aucun peer »,
// ce qui est faux et alarmant sur un écran d'administration.
public sealed class WireGuardVpnGateway(
    HttpClient httpClient
) : IVpnGateway
{
    public async Task<IReadOnlyList<VpnPeerDto>> FindPeersAsync()
    {
        using var response = await CallAsync(() => httpClient.GetAsync("peers"));
        var payload = await ReadAsync<WgApiPeersResponse>(response);

        return payload.Peers ?? throw AppException.Unavailable(
            "VPN_GATEWAY_INVALID_RESPONSE",
            "Le service WireGuard n'a retourné aucune liste de peers."
        );
    }

    public async Task<VpnPeerDto> AddPeerAsync(string name)
    {
        // Corps sérialisé d'avance : wg_api ne lit que Content-Length, et un JsonContent part en
        // Transfer-Encoding: chunked — le nom arriverait vide et serait refusé comme invalide.
        using var content = new StringContent(
            JsonSerializer.Serialize(new { name }),
            Encoding.UTF8,
            "application/json"
        );

        using var created = await CallAsync(() => httpClient.PostAsync("peers", content));

        // La création ne renvoie que { name, ip, config, qrcodePngBase64 } : de quoi écrire un
        // fichier client, pas de quoi remplir un VpnPeerDto. Seule la liste porte l'état réel.
        var peers = await FindPeersAsync();

        return peers.FirstOrDefault(peer => peer.Name == name) ?? throw AppException.Unavailable(
            "VPN_GATEWAY_INVALID_RESPONSE",
            $"Le peer « {name} » a été créé mais n'apparaît pas dans la liste du service WireGuard."
        );
    }

    public async Task RemovePeerAsync(string name)
    {
        using var response = await CallAsync(() => httpClient.DeleteAsync($"peers/{Uri.EscapeDataString(name)}"));
    }

    private static async Task<HttpResponseMessage> CallAsync(Func<Task<HttpResponseMessage>> call)
    {
        HttpResponseMessage response;

        try
        {
            response = await call();
        }
        catch (HttpRequestException ex)
        {
            throw AppException.Unavailable(
                "VPN_GATEWAY_UNAVAILABLE",
                $"Le service WireGuard est injoignable : {ex.Message}"
            );
        }
        catch (TaskCanceledException ex)
        {
            throw AppException.Unavailable(
                "VPN_GATEWAY_TIMEOUT",
                $"L'appel au service WireGuard a expiré : {ex.Message}"
            );
        }

        if (!response.IsSuccessStatusCode)
        {
            using (response)
            {
                throw await ToAppExceptionAsync(response);
            }
        }

        return response;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>() ?? throw AppException.Unavailable(
                "VPN_GATEWAY_INVALID_RESPONSE",
                "Le service WireGuard a retourné un corps vide."
            );
        }
        catch (JsonException ex)
        {
            throw AppException.Unavailable(
                "VPN_GATEWAY_INVALID_RESPONSE",
                $"La réponse du service WireGuard est invalide : {ex.Message}"
            );
        }
    }

    // Le code métier du service doit survivre au passage : sans ce mappage, un doublon ou un nom
    // inconnu se lirait comme une panne, et les deux adapters ne se comporteraient pas pareil.
    private static async Task<AppException> ToAppExceptionAsync(HttpResponseMessage response)
    {
        WgApiError? error = null;

        try
        {
            error = await response.Content.ReadFromJsonAsync<WgApiError>();
        }
        catch (JsonException)
        {
            // Corps illisible : le statut HTTP reste exploitable, on s'en contente.
        }

        var code = $"VPN_{error?.Error ?? "GATEWAY_ERROR"}";
        var message = error?.Message
            ?? $"Le service WireGuard a répondu {(int)response.StatusCode}.";

        return response.StatusCode switch
        {
            HttpStatusCode.Conflict => AppException.Conflict(code, message),
            HttpStatusCode.NotFound => AppException.NotFound(code, message),
            HttpStatusCode.BadRequest => AppException.Validation(code, message),
            _ => AppException.Unavailable(code, message),
        };
    }
}
