namespace Tools.Api.Modules.Core.Vpn.Infrastructure;

// Réponse d'erreur du service WireGuard : { "error": "PEER_EXISTS", "message": "…" }.
public sealed record WgApiError(string? Error, string? Message);
