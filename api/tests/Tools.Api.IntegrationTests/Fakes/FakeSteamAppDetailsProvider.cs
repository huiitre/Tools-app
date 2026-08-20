using Tools.Api.Modules.Core.GameServers.Application.Dto;
using Tools.Api.Modules.Core.GameServers.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class FakeSteamAppDetailsProvider : ISteamAppDetailsProvider
{
    private readonly Dictionary<int, SteamAppDetailsLookup> details = [];

    public void Set(int steamAppId, string? gameName, string? headerImageUrl) =>
        details[steamAppId] = new SteamAppDetailsLookup(true, gameName, headerImageUrl);

    public void SetUnavailable(int steamAppId) =>
        details[steamAppId] = new SteamAppDetailsLookup(false, null, null);

    public Task<SteamAppDetailsLookup> FindAsync(int steamAppId) => Task.FromResult(
        details.GetValueOrDefault(steamAppId, SteamAppDetailsLookup.AvailableWithoutMetadata));
}
