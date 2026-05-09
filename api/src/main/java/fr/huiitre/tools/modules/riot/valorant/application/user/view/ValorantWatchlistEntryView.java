package fr.huiitre.tools.modules.riot.valorant.application.user.view;

import java.time.LocalDateTime;

public record ValorantWatchlistEntryView(Long id, Long skinId, String skinName, String skinIconUrl, LocalDateTime addedAt) {}
