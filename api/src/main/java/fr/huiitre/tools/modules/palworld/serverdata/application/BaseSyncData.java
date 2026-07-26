package fr.huiitre.tools.modules.palworld.serverdata.application;

import java.util.UUID;

public class BaseSyncData {

    private final UUID baseId;
    private final UUID guildId;

    public BaseSyncData(UUID baseId, UUID guildId) {
        this.baseId = baseId;
        this.guildId = guildId;
    }

    public UUID getBaseId() { return baseId; }
    public UUID getGuildId() { return guildId; }
}
