namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Views;

public sealed record ValorantAccountView(
    long Id,
    string Puuid,
    string Region,
    string? GameName,
    string? TagLine,
    string? Label
);
