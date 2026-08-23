using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Views;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Domain;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;

public interface IExpeditionRepository
{
    Task<List<ExpeditionSummaryView>> FindAllByUserId(long userId);
    Task<ExpeditionDetailView?> FindByIdAndUserId(Guid expeditionId, long userId);
    Task<string?> FindRouteDataByIdAndUserId(Guid expeditionId, long userId);
    Task<Guid> Save(long userId, Expedition expedition);
    Task UpdateProgress(Guid id, long userId, int currentSystemIndex, List<long> currentBodiesDone);
    Task Rename(Guid id, long userId, string name);
    Task Delete(Guid id, long userId);
    Task<bool> ExistsByIdAndUserId(Guid id, long userId);
    Task<int> CountByUserId(long userId);
}
