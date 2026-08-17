package fr.huiitre.tools.modules.palworld.serverdata.application;

import java.time.OffsetDateTime;
import java.util.List;

public class ServerSnapshotSyncData {

    private final OffsetDateTime extractedAt;
    private final List<GuildSyncData> guilds;
    private final List<BaseSyncData> bases;
    private final List<PalInstanceSyncData> palInstances;

    public ServerSnapshotSyncData(OffsetDateTime extractedAt, List<GuildSyncData> guilds, List<BaseSyncData> bases,
            List<PalInstanceSyncData> palInstances) {
        this.extractedAt = extractedAt;
        this.guilds = guilds;
        this.bases = bases;
        this.palInstances = palInstances;
    }

    public OffsetDateTime getExtractedAt() { return extractedAt; }
    public List<GuildSyncData> getGuilds() { return guilds; }
    public List<BaseSyncData> getBases() { return bases; }
    public List<PalInstanceSyncData> getPalInstances() { return palInstances; }
}
