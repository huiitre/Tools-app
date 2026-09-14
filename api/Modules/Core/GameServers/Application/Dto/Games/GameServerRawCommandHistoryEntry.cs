namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

public sealed record GameServerRawCommandHistoryEntry(
    string Command,
    string? Answer,
    string UserName,
    DateTime ExecutedAt);
