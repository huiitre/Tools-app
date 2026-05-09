package fr.huiitre.tools.modules.riot.valorant.application.user.view;

import java.time.LocalDate;

public record ValorantStoreHistoryView(
    Long id,
    Long skinId,
    String skinName,
    String skinIconUrl,
    LocalDate seenAt
) {}
