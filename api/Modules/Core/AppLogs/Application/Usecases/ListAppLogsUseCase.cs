using Tools.Api.Modules.Core.AppLogs.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.AppLogs.Application.Usecases;

// Lecture globale, protégée par ADMIN et sans aucun chemin de mutation du journal.
public sealed class ListAppLogsUseCase(
    UseCaseAuthorizer authorizer,
    IAppLogRepository appLogRepository,
    IGeoIpLookup geoIpLookup) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task<AppLogPageDto> Execute(AppLogListQuery query)
    {
        ValidateRange(query.UserRegisteredFrom, query.UserRegisteredTo, "APP_LOG_USER_REGISTRATION_RANGE_INVALID", "La période d'inscription est invalide.");
        ValidateRange(query.CreatedFrom, query.CreatedTo, "APP_LOG_CREATED_RANGE_INVALID", "La période de création est invalide.");

        var page = await appLogRepository.FindForAdminAsync(query with
        {
            Search = EmptyToNull(query.Search),
            AreaCodes = NormalizeCodes(query.AreaCodes),
            ActionCodes = NormalizeCodes(query.ActionCodes),
            IpAddress = EmptyToNull(query.IpAddress),
            // Npgsql exige un DateTimeOffset en UTC pour un paramètre PostgreSQL timestamptz.
            CreatedFrom = query.CreatedFrom?.ToUniversalTime(),
            CreatedTo = query.CreatedTo?.ToUniversalTime()
        });

        // Une IP répétée sur la page (plusieurs actions du même utilisateur) n'est résolue qu'une fois :
        // Find() fait de l'I/O mmap bloquante par appel.
        var locationByIp = page.Items
            .Select(log => log.IpAddress)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(ip => ip, geoIpLookup.Find, StringComparer.Ordinal);
        return page with
        {
            Items = page.Items
                .Select(log => log with { IpLocation = log.IpAddress is { } ip ? locationByIp[ip] : null })
                .ToList()
        };
    }

    private static void ValidateRange<T>(T? from, T? to, string code, string message) where T : struct, IComparable<T>
    {
        if (from is { } start && to is { } end && start.CompareTo(end) > 0)
        {
            throw AppException.Validation(code, message);
        }
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string[]? NormalizeCodes(IEnumerable<string>? values) => values?
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim().ToUpperInvariant())
        .Distinct(StringComparer.Ordinal)
        .ToArray() is { Length: > 0 } normalized ? normalized : null;
}
