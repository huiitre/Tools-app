package fr.huiitre.tools.modules.palworld.sync.application;

public class PalPassiveSkillSyncData {

    private final String name;
    private final String tooltip;
    private final String rankIconUrl;

    public PalPassiveSkillSyncData(String name, String tooltip, String rankIconUrl) {
        this.name = name;
        this.tooltip = tooltip;
        this.rankIconUrl = rankIconUrl;
    }

    public String getName() { return name; }
    public String getTooltip() { return tooltip; }
    public String getRankIconUrl() { return rankIconUrl; }
}
