namespace Tools.Api.Modules.Core.AppLogs.Application;

public sealed record AppLogListQuery(
    int Page, int PageSize, long? UserId, string? UserSearch, long? RoleId, bool? UserActive,
    DateTime? UserRegisteredFrom, DateTime? UserRegisteredTo, long? ModuleId, string? AreaCode,
    string? ActionCode, string? IpAddress, bool? HasMetadata, DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo, AppLogSortColumn SortBy, SortDirection SortDirection)
{
    public long Offset => (long)(Page - 1) * PageSize;
}

public enum AppLogSortColumn
{
    CreatedAt, UserName, UserEmail, UserRole, UserStatus, UserRegisteredAt, ModuleName,
    AreaCode, ActionCode, IpAddress, UserAgent, HasMetadata
}

public enum SortDirection { Asc, Desc }
