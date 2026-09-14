namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Null quand le protocole ferme la connexion sans répondre (ex. une commande qui arrête le
// serveur) : ce n'est pas un échec, juste l'absence de réponse.
public sealed record GameServerRawCommandResult(string? Answer);
