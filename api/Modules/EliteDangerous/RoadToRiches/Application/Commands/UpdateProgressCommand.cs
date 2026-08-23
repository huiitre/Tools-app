namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Commands;

public sealed record UpdateProgressCommand(
    int CurrentSystemIndex,
    List<long> CurrentBodiesDone
);