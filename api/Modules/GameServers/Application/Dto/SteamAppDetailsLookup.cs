namespace Tools.Api.Modules.GameServers.Application.Dto;

public sealed record SteamAppDetailsLookup(bool IsAvailable, string? GameName, string? HeaderImageUrl)
{
    public static readonly SteamAppDetailsLookup AvailableWithoutMetadata = new(true, null, null);
}
