package fr.huiitre.tools.modules.palworld.serverdata.application;

import java.util.List;
import java.util.UUID;

public class GuildSyncData {

    private final UUID guildId;
    private final String name;
    private final List<PlayerSyncData> players;

    public GuildSyncData(UUID guildId, String name, List<PlayerSyncData> players) {
        this.guildId = guildId;
        this.name = name;
        this.players = players;
    }

    public UUID getGuildId() { return guildId; }
    public String getName() { return name; }
    public List<PlayerSyncData> getPlayers() { return players; }
}
