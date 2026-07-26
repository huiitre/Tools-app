package fr.huiitre.tools.modules.palworld.sync.application;

import java.util.List;

public class PalPartnerSkillSyncData {

    private final String title;
    private final String description;
    private final String iconUrl;
    private final List<PalPartnerSkillRankSyncData> ranks;

    public PalPartnerSkillSyncData(String title, String description, String iconUrl, List<PalPartnerSkillRankSyncData> ranks) {
        this.title = title;
        this.description = description;
        this.iconUrl = iconUrl;
        this.ranks = ranks;
    }

    public String getTitle() { return title; }
    public String getDescription() { return description; }
    public String getIconUrl() { return iconUrl; }
    public List<PalPartnerSkillRankSyncData> getRanks() { return ranks; }
}
