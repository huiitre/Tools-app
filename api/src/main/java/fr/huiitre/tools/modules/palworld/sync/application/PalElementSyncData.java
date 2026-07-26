package fr.huiitre.tools.modules.palworld.sync.application;

public class PalElementSyncData {

    private final String externalCode;
    private final String iconUrl;
    private final int sortOrder;

    public PalElementSyncData(String externalCode, String iconUrl, int sortOrder) {
        this.externalCode = externalCode;
        this.iconUrl = iconUrl;
        this.sortOrder = sortOrder;
    }

    public String getExternalCode() { return externalCode; }
    public String getIconUrl() { return iconUrl; }
    public int getSortOrder() { return sortOrder; }
}
