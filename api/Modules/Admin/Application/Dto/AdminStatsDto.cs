namespace Tools.Api.Modules.Admin.Application.Dto;

// Indicateurs du tableau de bord d'administration.
public sealed record AdminStatsDto(
    long TotalUsers,
    long ActiveUsers,
    long NewUsersThisWeek,
    IReadOnlyList<ModuleUserCountDto> UsersPerModule
);
