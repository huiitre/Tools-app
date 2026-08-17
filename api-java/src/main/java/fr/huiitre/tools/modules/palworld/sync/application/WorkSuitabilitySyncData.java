package fr.huiitre.tools.modules.palworld.sync.application;

public class WorkSuitabilitySyncData {

    private final String externalCode;
    private final String slug;
    private final String name;
    private final String iconUrl;

    public WorkSuitabilitySyncData(String externalCode, String slug, String name, String iconUrl) {
        this.externalCode = externalCode;
        this.slug = slug;
        this.name = name;
        this.iconUrl = iconUrl;
    }

    public String getExternalCode() { return externalCode; }
    public String getSlug() { return slug; }
    public String getName() { return name; }
    public String getIconUrl() { return iconUrl; }
}
