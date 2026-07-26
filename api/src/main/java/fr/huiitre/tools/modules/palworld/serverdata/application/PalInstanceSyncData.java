package fr.huiitre.tools.modules.palworld.serverdata.application;

import java.math.BigDecimal;
import java.util.UUID;

public class PalInstanceSyncData {

    private final UUID instanceId;
    private final String characterId;
    private final UUID ownerPlayerUid;
    private final UUID baseId;
    private final Integer level;
    private final Integer exp;
    private final BigDecimal fullStomach;
    private final Boolean isSick;
    private final String workableType;
    private final String taskId;
    private final Integer workState;
    private final BigDecimal currentWorkAmount;
    private final BigDecimal requiredWorkAmount;

    public PalInstanceSyncData(UUID instanceId, String characterId, UUID ownerPlayerUid, UUID baseId, Integer level,
            Integer exp, BigDecimal fullStomach, Boolean isSick, String workableType, String taskId, Integer workState,
            BigDecimal currentWorkAmount, BigDecimal requiredWorkAmount) {
        this.instanceId = instanceId;
        this.characterId = characterId;
        this.ownerPlayerUid = ownerPlayerUid;
        this.baseId = baseId;
        this.level = level;
        this.exp = exp;
        this.fullStomach = fullStomach;
        this.isSick = isSick;
        this.workableType = workableType;
        this.taskId = taskId;
        this.workState = workState;
        this.currentWorkAmount = currentWorkAmount;
        this.requiredWorkAmount = requiredWorkAmount;
    }

    public UUID getInstanceId() { return instanceId; }
    public String getCharacterId() { return characterId; }
    public UUID getOwnerPlayerUid() { return ownerPlayerUid; }
    public UUID getBaseId() { return baseId; }
    public Integer getLevel() { return level; }
    public Integer getExp() { return exp; }
    public BigDecimal getFullStomach() { return fullStomach; }
    public Boolean getIsSick() { return isSick; }
    public String getWorkableType() { return workableType; }
    public String getTaskId() { return taskId; }
    public Integer getWorkState() { return workState; }
    public BigDecimal getCurrentWorkAmount() { return currentWorkAmount; }
    public BigDecimal getRequiredWorkAmount() { return requiredWorkAmount; }

    public boolean isAlpha() {
        return characterId != null && characterId.toUpperCase().startsWith("BOSS_");
    }

    public String characterIdWithoutBossPrefix() {
        if (characterId == null) return null;
        return isAlpha() ? characterId.substring("BOSS_".length()) : characterId;
    }
}
