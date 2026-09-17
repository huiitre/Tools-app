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
            request.CreatedFrom, request.CreatedTo, ParseSortBy(request.SortBy), ParseSortDirection(request.SortDirection)));

    private static AppLogSortColumn ParseSortBy(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "createdat" => AppLogSortColumn.CreatedAt,
        "username" => AppLogSortColumn.UserName,
        "useremail" => AppLogSortColumn.UserEmail,
        "userrole" => AppLogSortColumn.UserRole,
        "userstatus" => AppLogSortColumn.UserStatus,
        "userregisteredat" => AppLogSortColumn.UserRegisteredAt,
        "modulename" => AppLogSortColumn.ModuleName,
        "areacode" => AppLogSortColumn.AreaCode,
        "actioncode" => AppLogSortColumn.ActionCode,
        "ipaddress" => AppLogSortColumn.IpAddress,
        "useragent" => AppLogSortColumn.UserAgent,
        "hasmetadata" => AppLogSortColumn.HasMetadata,
        _ => throw AppException.Validation("APP_LOG_SORT_INVALID", "Colonne de tri du journal invalide.")
    };

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
    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}
