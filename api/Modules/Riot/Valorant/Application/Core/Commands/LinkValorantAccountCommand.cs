namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Commands;

public sealed record LinkValorantAccountCommand(
    string RefreshToken,
    string Region,
    string? Label
);
