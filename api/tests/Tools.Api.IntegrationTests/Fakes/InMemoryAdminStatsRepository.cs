using Tools.Api.Modules.Admin.Application.Dto;
using Tools.Api.Modules.Admin.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

// Indicateurs figés : les tests vérifient le droit d'accès et la forme de la réponse,
// pas le calcul SQL.
public sealed class InMemoryAdminStatsRepository : IAdminStatsRepository
{
    public Task<AdminStatsDto> GetStatsAsync() =>
        Task.FromResult(new AdminStatsDto(
            TotalUsers: 12,
            ActiveUsers: 9,
            NewUsersThisWeek: 3,
            UsersPerModule: [new ModuleUserCountDto("dofus", "Dofus", 7)]));
}
