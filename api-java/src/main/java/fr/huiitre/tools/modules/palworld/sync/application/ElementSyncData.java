package fr.huiitre.tools.modules.palworld.sync.application;

public class ElementSyncData {

    private final String externalCode;
    private final String code;
    private final String palElementType;
    private final String name;
    private final String iconUrl;

    public ElementSyncData(String externalCode, String code, String palElementType, String name, String iconUrl) {
        this.externalCode = externalCode;
        this.code = code;
        this.palElementType = palElementType;
        this.name = name;
        this.iconUrl = iconUrl;
    }

    public String getExternalCode() { return externalCode; }
    public String getCode() { return code; }
    public String getPalElementType() { return palElementType; }
    public String getName() { return name; }
    public String getIconUrl() { return iconUrl; }
}
