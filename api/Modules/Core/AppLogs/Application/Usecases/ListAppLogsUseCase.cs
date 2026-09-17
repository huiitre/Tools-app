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
        if (query.UserRegisteredFrom is { } registeredFrom && query.UserRegisteredTo is { } registeredTo
            && registeredFrom > registeredTo)
        {
            throw AppException.Validation("APP_LOG_USER_REGISTRATION_RANGE_INVALID", "La période d'inscription est invalide.");
        }

        if (query.CreatedFrom is { } createdFrom && query.CreatedTo is { } createdTo && createdFrom > createdTo)
        {
            throw AppException.Validation("APP_LOG_CREATED_RANGE_INVALID", "La période de création est invalide.");
        }

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
        return page with { Items = page.Items.Select(log => log with { IpLocation = geoIpLookup.Find(log.IpAddress) }).ToList() };
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string[]? NormalizeCodes(IEnumerable<string>? values) => values?
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim().ToUpperInvariant())
        .Distinct(StringComparer.Ordinal)
        .ToArray() is { Length: > 0 } normalized ? normalized : null;
}
