package fr.huiitre.tools.modules.palworld.sync.application;

public class PalSpawnZoneSyncData {

    private final String levelLabel;
    private final String locationLabel;
    private final String locationLink;
    private final int sortOrder;

    public PalSpawnZoneSyncData(String levelLabel, String locationLabel, String locationLink, int sortOrder) {
        this.levelLabel = levelLabel;
        this.locationLabel = locationLabel;
        this.locationLink = locationLink;
        this.sortOrder = sortOrder;
    }

    public String getLevelLabel() { return levelLabel; }
    public String getLocationLabel() { return locationLabel; }
    public String getLocationLink() { return locationLink; }
    public int getSortOrder() { return sortOrder; }
}
