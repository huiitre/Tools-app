package fr.huiitre.tools.modules.riot.valorant.application.user.view;

import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;
import java.time.LocalDate;
import java.util.List;

public record ValorantStoreHistoryView(
    LocalDate date,
    List<ValorantSkinView> skins
) {}
