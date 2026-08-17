package fr.huiitre.tools.modules.palworld.serverdata.application.view;

import java.time.OffsetDateTime;
import java.util.List;

public record ServerDataInventoryView(
        OffsetDateTime lastSyncedAt,
        List<GuildSummaryView> guilds,
        List<ServerPalInventoryView> pals) {}
