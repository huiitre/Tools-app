package fr.huiitre.tools.modules.palworld.sync.application;

public class PalElementSyncData {

    private final String elementName;
    private final String iconUrl;
    private final int sortOrder;

    public PalElementSyncData(String elementName, String iconUrl, int sortOrder) {
        this.elementName = elementName;
        this.iconUrl = iconUrl;
        this.sortOrder = sortOrder;
    }

    public String getElementName() { return elementName; }
    public String getIconUrl() { return iconUrl; }
    public int getSortOrder() { return sortOrder; }
}
