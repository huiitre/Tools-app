namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;

public sealed record SteamAppDetailsLookup(bool IsAvailable, string? GameName, string? HeaderImageUrl)
{
    public static readonly SteamAppDetailsLookup AvailableWithoutMetadata = new(true, null, null);
}
