namespace Tools.Api.Modules.GameServers.Application.Ports;

public interface ISteamAppDetailsProvider
{
    Task<SteamAppDetailsLookup> FindAsync(int steamAppId);
}

public sealed record SteamAppDetailsLookup(bool IsAvailable, string? GameName, string? HeaderImageUrl)
{
    public static readonly SteamAppDetailsLookup AvailableWithoutMetadata = new(true, null, null);
}
