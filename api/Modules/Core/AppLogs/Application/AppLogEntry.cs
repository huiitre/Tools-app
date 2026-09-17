using System.Net;

namespace Tools.Api.Modules.Core.AppLogs.Application;

// Ligne prête à persister, après normalisation des codes et enrichissement par le contexte HTTP.
public sealed record AppLogEntry(
    long? ModuleId,
    string AreaCode,
    string ActionCode,
    long? UserId,
    IPAddress? IpAddress,
    string? UserAgent,
    string Metadata);
