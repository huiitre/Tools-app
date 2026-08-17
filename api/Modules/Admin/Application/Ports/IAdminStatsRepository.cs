using Tools.ApiCore.Modules.Admin.Application.Dto;

namespace Tools.ApiCore.Modules.Admin.Application.Ports;

public interface IAdminStatsRepository
{
    Task<AdminStatsDto> GetStatsAsync();
}
