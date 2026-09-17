namespace Tools.Api.Modules.Core.AppLogs.Application.Ports;

public interface IGeoIpLookup
{
    AppLogIpLocationDto? Find(string? ipAddress);
}
