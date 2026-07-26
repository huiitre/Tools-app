package fr.huiitre.tools.modules.palworld.sync.application;

public class PalActiveSkillSyncData {

    private final String skillSlug;
    private final int unlockLevel;
    private final int sortOrder;

    public PalActiveSkillSyncData(String skillSlug, int unlockLevel, int sortOrder) {
        this.skillSlug = skillSlug;
        this.unlockLevel = unlockLevel;
        this.sortOrder = sortOrder;
    }

    public String getSkillSlug() { return skillSlug; }
    public int getUnlockLevel() { return unlockLevel; }
    public int getSortOrder() { return sortOrder; }
}
