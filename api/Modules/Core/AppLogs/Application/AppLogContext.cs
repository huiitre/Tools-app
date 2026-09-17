using System.Net;

namespace Tools.Api.Modules.Core.AppLogs.Application;

// Contexte technique minimal qui accompagne un événement applicatif. Les tâches de fond
// obtiennent naturellement deux valeurs nulles puisqu'elles n'ont aucune requête HTTP courante.
public sealed record AppLogContext(IPAddress? IpAddress, string? UserAgent);
