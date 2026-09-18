using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Tools.Api.Modules.Core.AppLogs.Application;
using Tools.Api.Modules.Core.AppLogs.Application.Usecases;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.AppLogs.Api;

[ApiController]
[Route("admin/app-logs")]
public sealed class AppLogsController : ControllerBase
{
    [HttpGet]
    public Task<AppLogPageDto> List(
        [FromQuery] ListAppLogsRequest request,
        [FromServices] ListAppLogsUseCase listAppLogsUseCase) =>
        listAppLogsUseCase.Execute(new AppLogListQuery(
            request.Page, request.PageSize, request.UserIds, request.Search, request.RoleId,
            request.UserActive, request.UserRegisteredFrom, request.UserRegisteredTo, request.ModuleIds,
            request.AreaCodes, request.ActionCodes, request.IpAddress, request.HasMetadata,
            ToUtcStartOfDay(request.CreatedFrom), ToUtcStartOfDay(request.CreatedTo),
            ParseSortBy(request.SortBy), ParseSortDirection(request.SortDirection)));

    // Le sélecteur front n'envoie qu'un jour calendaire (`type="date"`, sans heure ni fuseau) : le lier
    // en DateTimeOffset ferait résoudre l'offset manquant par ASP.NET avec le fuseau *du serveur*, pas
    // UTC ni celui de l'admin. En DateOnly il n'y a rien à deviner ; on fixe nous-mêmes minuit UTC.
    private static DateTimeOffset? ToUtcStartOfDay(DateOnly? date) =>
        date is { } value ? new DateTimeOffset(value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    private static AppLogSortColumn ParseSortBy(string? value) =>
        string.IsNullOrWhiteSpace(value) ? AppLogSortColumn.CreatedAt :
        Enum.TryParse<AppLogSortColumn>(value, ignoreCase: true, out var column) ? column :
        throw AppException.Validation("APP_LOG_SORT_INVALID", "Colonne de tri du journal invalide.");

    private static SortDirection ParseSortDirection(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "desc" => SortDirection.Desc,
        "asc" => SortDirection.Asc,
        _ => throw AppException.Validation("APP_LOG_SORT_DIRECTION_INVALID", "Sens de tri du journal invalide.")
    };
}

public sealed class ListAppLogsRequest
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 50;
    public long[]? UserIds { get; init; }
    public string? Search { get; init; }
    public long? RoleId { get; init; }
    public bool? UserActive { get; init; }
    public DateTime? UserRegisteredFrom { get; init; }
    public DateTime? UserRegisteredTo { get; init; }
    public long[]? ModuleIds { get; init; }
    public string[]? AreaCodes { get; init; }
    public string[]? ActionCodes { get; init; }
    public string? IpAddress { get; init; }
    public bool? HasMetadata { get; init; }
    public DateOnly? CreatedFrom { get; init; }
    public DateOnly? CreatedTo { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}
