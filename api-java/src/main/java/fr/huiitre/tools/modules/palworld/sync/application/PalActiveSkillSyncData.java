package fr.huiitre.tools.modules.palworld.sync.application;

public class PalActiveSkillSyncData {

    private final String skillName;
    private final int unlockLevel;
    private final int sortOrder;

    public PalActiveSkillSyncData(String skillName, int unlockLevel, int sortOrder) {
        this.skillName = skillName;
        this.unlockLevel = unlockLevel;
        this.sortOrder = sortOrder;
    }

    public String getSkillName() { return skillName; }
    public int getUnlockLevel() { return unlockLevel; }
    public int getSortOrder() { return sortOrder; }
}
