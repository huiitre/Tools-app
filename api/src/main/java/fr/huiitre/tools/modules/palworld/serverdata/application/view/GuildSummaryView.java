package fr.huiitre.tools.modules.palworld.serverdata.application.view;

import java.util.List;
import java.util.UUID;

public record GuildSummaryView(UUID guildId, String name, List<PlayerSummaryView> players, List<BaseSummaryView> bases) {}
