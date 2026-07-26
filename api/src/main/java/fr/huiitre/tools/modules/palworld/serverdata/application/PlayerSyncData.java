package fr.huiitre.tools.modules.palworld.serverdata.application;

import java.util.UUID;

public class PlayerSyncData {

    private final UUID playerUid;
    private final String name;
    private final Long lastOnlineRealTime;

    public PlayerSyncData(UUID playerUid, String name, Long lastOnlineRealTime) {
        this.playerUid = playerUid;
        this.name = name;
        this.lastOnlineRealTime = lastOnlineRealTime;
    }

    public UUID getPlayerUid() { return playerUid; }
    public String getName() { return name; }
    public Long getLastOnlineRealTime() { return lastOnlineRealTime; }
}
