package fr.huiitre.tools.modules.palworld.sync.application;

public class PalWorkSuitabilitySyncData {

    private final String slug;
    private final String iconUrl;
    private final int level;

    public PalWorkSuitabilitySyncData(String slug, String iconUrl, int level) {
        this.slug = slug;
        this.iconUrl = iconUrl;
        this.level = level;
    }

    public String getSlug() { return slug; }
    public String getIconUrl() { return iconUrl; }
    public int getLevel() { return level; }
}
