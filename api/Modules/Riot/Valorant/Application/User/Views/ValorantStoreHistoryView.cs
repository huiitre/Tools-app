using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.User.Views;

public sealed record ValorantStoreHistoryView(
    DateOnly Date,
    List<ValorantSkinView> Skins
);
