using Tools.Api.Modules.Core.AppLogs.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.AppLogs.Application.Usecases;

// Lecture globale, protégée par ADMIN et sans aucun chemin de mutation du journal.
public sealed class ListAppLogsUseCase(
    UseCaseAuthorizer authorizer,
    IAppLogRepository appLogRepository) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public Task<AppLogPageDto> Execute(AppLogListQuery query)
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

        return appLogRepository.FindForAdminAsync(query with
        {
            UserSearch = EmptyToNull(query.UserSearch),
            AreaCode = NormalizeCode(query.AreaCode),
            ActionCode = NormalizeCode(query.ActionCode),
            IpAddress = EmptyToNull(query.IpAddress)
        });
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizeCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
