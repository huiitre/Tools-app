using Tools.Api.Modules.Core.Admin.Application.Dto;

namespace Tools.Api.Modules.Core.Admin.Application.Ports;

public interface IAdminStatsRepository
{
    Task<AdminStatsDto> GetStatsAsync();
}
