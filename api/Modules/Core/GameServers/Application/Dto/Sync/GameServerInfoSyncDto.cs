namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;

// Une information libre du manifest ; label et value sont tous deux exigés.
public sealed record GameServerInfoSyncDto(string? Label, string? Value);
