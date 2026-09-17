using System.Text.Json;
using Tools.Api.Modules.Core.AppLogs.Application.Ports;

namespace Tools.Api.Modules.Core.AppLogs.Application.Services;

// Point d'entrée transverse du journal applicatif. Les appelants décrivent l'événement ; le
// service normalise sa classification, sérialise son contexte et ajoute les données HTTP.
public sealed class AppLogService(
    IAppLogRepository appLogRepository,
    IAppLogContextProvider contextProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<long> Log(AppLogCommand command)
    {
        var context = contextProvider.Current;
        var entry = new AppLogEntry(
            command.ModuleId,
            Normalize(command.AreaCode, nameof(command.AreaCode)),
            Normalize(command.ActionCode, nameof(command.ActionCode)),
            command.UserId,
            context.IpAddress,
            context.UserAgent,
            command.Metadata is null ? "{}" : JsonSerializer.Serialize(command.Metadata, JsonOptions));

        return appLogRepository.InsertAsync(entry);
    }

    private static string Normalize(string code, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code du journal ne peut pas être vide.", parameterName);
        }

        return code.Trim().ToUpperInvariant();
    }
}
