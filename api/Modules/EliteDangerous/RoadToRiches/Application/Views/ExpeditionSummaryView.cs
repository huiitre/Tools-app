namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Views;

public sealed record ExpeditionSummaryView(
    Guid Id,
    string Name,
    string Source,
    int CurrentSystemIndex,
    int TotalSystems,
    DateTime CreatedAt,
    DateTime UpdatedAt
);