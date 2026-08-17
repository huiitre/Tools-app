using Tools.Api.Modules.Admin.Application.Dto;

namespace Tools.Api.Modules.Admin.Application.Ports;

public interface IAdminStatsRepository
{
    Task<AdminStatsDto> GetStatsAsync();
}
