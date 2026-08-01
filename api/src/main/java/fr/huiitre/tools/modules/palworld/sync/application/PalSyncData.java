package fr.huiitre.tools.modules.palworld.sync.application;

import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.List;

public class PalSyncData {

    private final String tribe;
    private final Integer paldexIndex;
    private final String paldexSuffix;
    private final String name;
    private final String imageUrl;
    private final String description;
    private final String size;
    private final Integer rarity;
    private final Integer baseHp;
    private final Integer baseAttack;
    private final Integer baseDefense;
    private final Integer baseWorkSpeed;
    private final Integer baseSupport;
    private final Integer foodAmount;
    private final Integer runSpeed;
    private final Integer rideSprintSpeed;
    private final BigDecimal captureRateCorrect;
    private final BigDecimal maleProbability;
    private final Integer combiRank;
    private final Integer goldCoin;
    private final String eggType;
    private final String bestWorkSuitabilityLabel;
    private final Integer foodGaugeFilled;
    private final Integer foodGaugeEmpty;
    private final String foodGaugeIconUrl;
    private final List<PalElementSyncData> elements;
    private final List<PalWorkSuitabilitySyncData> workSuitabilities;
    private final List<PalActiveSkillSyncData> activeSkills;
    private final List<PalPassiveSkillSyncData> passiveSkills;
    private final PalPartnerSkillSyncData partnerSkill;
    private final List<PalDropSyncData> drops;
    private final List<PalVariantSyncData> variants;
    private final List<PalSpawnZoneSyncData> spawnZones;
    private final String sourceSlug;
    private final String sourceUrl;
    private final String rawPayloadJson;
    private final OffsetDateTime fetchedAt;

    public PalSyncData(String tribe, Integer paldexIndex, String paldexSuffix, String name, String imageUrl, String description,
            String size, Integer rarity, Integer baseHp, Integer baseAttack, Integer baseDefense, Integer baseWorkSpeed,
            Integer baseSupport, Integer foodAmount, Integer runSpeed, Integer rideSprintSpeed, BigDecimal captureRateCorrect,
            BigDecimal maleProbability, Integer combiRank, Integer goldCoin,
            String eggType, String bestWorkSuitabilityLabel, Integer foodGaugeFilled, Integer foodGaugeEmpty,
            String foodGaugeIconUrl, List<PalElementSyncData> elements,
            List<PalWorkSuitabilitySyncData> workSuitabilities, List<PalActiveSkillSyncData> activeSkills,
            List<PalPassiveSkillSyncData> passiveSkills, PalPartnerSkillSyncData partnerSkill, List<PalDropSyncData> drops,
            List<PalVariantSyncData> variants, List<PalSpawnZoneSyncData> spawnZones, String sourceSlug, String sourceUrl,
            String rawPayloadJson, OffsetDateTime fetchedAt) {
        this.tribe = tribe;
        this.paldexIndex = paldexIndex;
        this.paldexSuffix = paldexSuffix;
        this.name = name;
        this.imageUrl = imageUrl;
        this.description = description;
        this.size = size;
        this.rarity = rarity;
        this.baseHp = baseHp;
        this.baseAttack = baseAttack;
        this.baseDefense = baseDefense;
        this.baseWorkSpeed = baseWorkSpeed;
        this.baseSupport = baseSupport;
        this.foodAmount = foodAmount;
        this.runSpeed = runSpeed;
        this.rideSprintSpeed = rideSprintSpeed;
        this.captureRateCorrect = captureRateCorrect;
        this.maleProbability = maleProbability;
        this.combiRank = combiRank;
        this.goldCoin = goldCoin;
        this.eggType = eggType;
        this.bestWorkSuitabilityLabel = bestWorkSuitabilityLabel;
        this.foodGaugeFilled = foodGaugeFilled;
        this.foodGaugeEmpty = foodGaugeEmpty;
        this.foodGaugeIconUrl = foodGaugeIconUrl;
        this.elements = elements;
        this.workSuitabilities = workSuitabilities;
        this.activeSkills = activeSkills;
        this.passiveSkills = passiveSkills;
        this.partnerSkill = partnerSkill;
        this.drops = drops;
        this.variants = variants;
        this.spawnZones = spawnZones;
        this.sourceSlug = sourceSlug;
        this.sourceUrl = sourceUrl;
        this.rawPayloadJson = rawPayloadJson;
        this.fetchedAt = fetchedAt;
    }

    public String getTribe() { return tribe; }
    public Integer getPaldexIndex() { return paldexIndex; }
    public String getPaldexSuffix() { return paldexSuffix; }
    public String getName() { return name; }
    public String getImageUrl() { return imageUrl; }
    public String getDescription() { return description; }
    public String getSize() { return size; }
    public Integer getRarity() { return rarity; }
    public Integer getBaseHp() { return baseHp; }
    public Integer getBaseAttack() { return baseAttack; }
    public Integer getBaseDefense() { return baseDefense; }
    public Integer getBaseWorkSpeed() { return baseWorkSpeed; }
    public Integer getBaseSupport() { return baseSupport; }
    public Integer getFoodAmount() { return foodAmount; }
    public Integer getRunSpeed() { return runSpeed; }
    public Integer getRideSprintSpeed() { return rideSprintSpeed; }
    public BigDecimal getCaptureRateCorrect() { return captureRateCorrect; }
    public BigDecimal getMaleProbability() { return maleProbability; }
    public Integer getCombiRank() { return combiRank; }
    public Integer getGoldCoin() { return goldCoin; }
    public String getEggType() { return eggType; }
    public String getBestWorkSuitabilityLabel() { return bestWorkSuitabilityLabel; }
    public Integer getFoodGaugeFilled() { return foodGaugeFilled; }
    public Integer getFoodGaugeEmpty() { return foodGaugeEmpty; }
    public String getFoodGaugeIconUrl() { return foodGaugeIconUrl; }
    public List<PalElementSyncData> getElements() { return elements; }
    public List<PalWorkSuitabilitySyncData> getWorkSuitabilities() { return workSuitabilities; }
    public List<PalActiveSkillSyncData> getActiveSkills() { return activeSkills; }
    public List<PalPassiveSkillSyncData> getPassiveSkills() { return passiveSkills; }
    public PalPartnerSkillSyncData getPartnerSkill() { return partnerSkill; }
    public List<PalDropSyncData> getDrops() { return drops; }
    public List<PalVariantSyncData> getVariants() { return variants; }
    public List<PalSpawnZoneSyncData> getSpawnZones() { return spawnZones; }
    public String getSourceSlug() { return sourceSlug; }
    public String getSourceUrl() { return sourceUrl; }
    public String getRawPayloadJson() { return rawPayloadJson; }
    public OffsetDateTime getFetchedAt() { return fetchedAt; }
}
