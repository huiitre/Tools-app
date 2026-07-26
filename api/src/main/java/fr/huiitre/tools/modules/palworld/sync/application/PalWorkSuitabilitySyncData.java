package fr.huiitre.tools.modules.palworld.sync.application;

public class PalWorkSuitabilitySyncData {

    private final String slug;
    private final int level;

    public PalWorkSuitabilitySyncData(String slug, int level) {
        this.slug = slug;
        this.level = level;
    }

    public String getSlug() { return slug; }
    public int getLevel() { return level; }
}
