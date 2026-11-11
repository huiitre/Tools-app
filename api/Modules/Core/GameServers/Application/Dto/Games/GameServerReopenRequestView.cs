namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// `NextRequestAt` : à partir de quand une nouvelle demande sera acceptée pour ce serveur.
public sealed record GameServerReopenRequestView(DateTime RequestedAt, DateTime NextRequestAt);
