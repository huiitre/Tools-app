package fr.huiitre.tools.modules.palworld.sync.application;

public class PalPartnerSkillRankSyncData {

    private final int sortOrder;
    private final String levelLabel;
    private final String detail;

    public PalPartnerSkillRankSyncData(int sortOrder, String levelLabel, String detail) {
        this.sortOrder = sortOrder;
        this.levelLabel = levelLabel;
        this.detail = detail;
    }

    public int getSortOrder() { return sortOrder; }
    public String getLevelLabel() { return levelLabel; }
    public String getDetail() { return detail; }
}
