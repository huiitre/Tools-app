using System.Net;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Tools.Api.Modules.Core.AppLogs.Application;
using Tools.Api.Modules.Core.AppLogs.Application.Ports;

namespace Tools.Api.Modules.Core.AppLogs.Infrastructure;

public sealed class MaxMindGeoIpLookup(IConfiguration configuration, ILogger<MaxMindGeoIpLookup> logger) : IGeoIpLookup, IDisposable
{
    private readonly string? databasePath = configuration["GeoIp:DatabasePath"];
    private readonly Lock gate = new();
    private volatile DatabaseReader? reader;
    private volatile bool warned;

    public AppLogIpLocationDto? Find(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out var address) || IsPrivate(address)) return null;
        try
        {
            var currentReader = GetReader();
            if (currentReader is null) return null;
            var city = currentReader.City(address);
            return new AppLogIpLocationDto(city.Country.IsoCode, French(city.Country.Names), French(city.City.Names));
        }
        catch (AddressNotFoundException) { return null; }
    }

    // Ne se fige jamais sur un échec : tant qu'aucun reader n'a pu être ouvert, chaque appel retente
    // (la base GeoLite2 peut arriver après le démarrage du process). Le verrou garantit qu'un seul
    // DatabaseReader est jamais ouvert, sans quoi le second écraserait la référence du premier et son
    // handle de fichier fuirait.
    private DatabaseReader? GetReader()
    {
        var current = reader;
        if (current is not null) return current;
        lock (gate)
        {
            return reader ??= OpenReader();
        }
    }

    private DatabaseReader? OpenReader()
    {
        if (string.IsNullOrWhiteSpace(databasePath) || !File.Exists(databasePath))
        {
            if (!warned)
            {
                logger.LogWarning("Base GeoLite2 absente : la localisation IP est désactivée ({Path}).", databasePath ?? "GeoIp:DatabasePath non configuré");
                warned = true;
            }
            return null;
        }
        return new DatabaseReader(databasePath);
    }

    private static string? French(IReadOnlyDictionary<string, string>? names) =>
        names is not null && names.TryGetValue("fr", out var french) ? french : names?.GetValueOrDefault("en");

    private static bool IsPrivate(IPAddress address) =>
        IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal ||
        (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && address.GetAddressBytes() is var bytes &&
            (bytes[0] == 10 || bytes[0] == 127 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168)));

    public void Dispose() => reader?.Dispose();
}
