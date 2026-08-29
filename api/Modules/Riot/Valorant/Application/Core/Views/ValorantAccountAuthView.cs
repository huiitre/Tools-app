namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Views;

public sealed record ValorantAccountAuthView(
    ValorantAccountView Account,
    string AccessToken
);
