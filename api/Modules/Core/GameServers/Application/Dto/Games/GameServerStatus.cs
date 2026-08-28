namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Résultat normalisé de tous les protocols. Null signifie "non disponible par ce protocole".
public sealed record GameServerStatus(bool Online, int? NumPlayers, int? MaxPlayers)
{
    public static GameServerStatus Offline { get; } = new(false, null, null);
}
