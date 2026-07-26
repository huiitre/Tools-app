package fr.huiitre.tools.modules.palworld.sync.application;

public class ElementSyncData {

    private final String externalCode;
    private final String name;
    private final String iconUrl;

    public ElementSyncData(String externalCode, String name, String iconUrl) {
        this.externalCode = externalCode;
        this.name = name;
        this.iconUrl = iconUrl;
    }

    public String getExternalCode() { return externalCode; }
    public String getName() { return name; }
    public String getIconUrl() { return iconUrl; }
}
