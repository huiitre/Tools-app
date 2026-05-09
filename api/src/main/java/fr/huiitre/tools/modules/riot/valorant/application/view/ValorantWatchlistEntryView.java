package fr.huiitre.tools.modules.riot.valorant.application.view;

import java.time.LocalDateTime;

public record ValorantWatchlistEntryView(Long id, Long skinId, String skinName, String skinIconUrl, LocalDateTime addedAt) {}
