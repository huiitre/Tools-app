namespace Tools.Api.Modules.Core.AppLogs.Application;

using System.Text.Json;

public sealed record AppLogPageDto(
    IReadOnlyList<AppLogAdminDto> Items, long TotalCount, int Page, int PageSize,
    AppLogFilterOptionsDto FilterOptions);

public sealed record AppLogFilterOptionsDto(
    IReadOnlyList<string> AreaCodes,
    IReadOnlyList<string> ActionCodes);

public sealed record AppLogAdminDto(
    long Id, DateTime CreatedAt, long? ModuleId, string? ModuleName,
    string AreaCode, string ActionCode, long? UserId, string? UserName, string? UserEmail,
    long? UserRoleId, string? UserRoleCode, bool? UserActive, DateTime? UserRegisteredAt,
    string? IpAddress, string? UserAgent, bool HasMetadata, JsonElement Metadata, AppLogIpLocationDto? IpLocation);
