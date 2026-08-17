package fr.huiitre.tools.modules.palworld.sync.application;

public class PalWorkSuitabilitySyncData {

    private final String slug;
    private final String iconUrl;
    private final int level;
    private final Integer maxLevel;
    private final Integer starSegments;
    private final Integer emptySegments;
    private final boolean priority;

    public PalWorkSuitabilitySyncData(String slug, String iconUrl, int level, Integer maxLevel, Integer starSegments,
            Integer emptySegments, boolean priority) {
        this.slug = slug;
        this.iconUrl = iconUrl;
        this.level = level;
        this.maxLevel = maxLevel;
        this.starSegments = starSegments;
        this.emptySegments = emptySegments;
        this.priority = priority;
    }

    public String getSlug() { return slug; }
    public String getIconUrl() { return iconUrl; }
    public int getLevel() { return level; }
    public Integer getMaxLevel() { return maxLevel; }
    public Integer getStarSegments() { return starSegments; }
    public Integer getEmptySegments() { return emptySegments; }
    public boolean isPriority() { return priority; }
}
