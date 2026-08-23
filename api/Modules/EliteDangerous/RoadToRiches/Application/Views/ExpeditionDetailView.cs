namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Views;

public sealed record ExpeditionDetailView(
    Guid Id,
    string Name,
    string Source,
    string RouteData,
    int CurrentSystemIndex,
    List<long> CurrentBodiesDone,
    DateTime CreatedAt,
    DateTime UpdatedAt
);