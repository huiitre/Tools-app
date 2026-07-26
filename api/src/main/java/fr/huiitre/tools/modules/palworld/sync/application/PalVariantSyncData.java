package fr.huiitre.tools.modules.palworld.sync.application;

public class PalVariantSyncData {

    private final String slug;
    private final String name;
    private final String iconUrl;
    private final String role;
    private final int sortOrder;

    public PalVariantSyncData(String slug, String name, String iconUrl, String role, int sortOrder) {
        this.slug = slug;
        this.name = name;
        this.iconUrl = iconUrl;
        this.role = role;
        this.sortOrder = sortOrder;
    }

    public String getSlug() { return slug; }
    public String getName() { return name; }
    public String getIconUrl() { return iconUrl; }
    public String getRole() { return role; }
    public int getSortOrder() { return sortOrder; }
}
