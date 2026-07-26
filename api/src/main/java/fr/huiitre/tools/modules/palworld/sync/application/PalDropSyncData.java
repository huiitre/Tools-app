package fr.huiitre.tools.modules.palworld.sync.application;

import java.math.BigDecimal;

public class PalDropSyncData {

    private final String itemSlug;
    private final String itemName;
    private final String itemIconUrl;
    private final Integer quantityMin;
    private final Integer quantityMax;
    private final BigDecimal probabilityPercent;
    private final String levelLabel;
    private final int sortOrder;

    public PalDropSyncData(String itemSlug, String itemName, String itemIconUrl, Integer quantityMin, Integer quantityMax,
            BigDecimal probabilityPercent, String levelLabel, int sortOrder) {
        this.itemSlug = itemSlug;
        this.itemName = itemName;
        this.itemIconUrl = itemIconUrl;
        this.quantityMin = quantityMin;
        this.quantityMax = quantityMax;
        this.probabilityPercent = probabilityPercent;
        this.levelLabel = levelLabel;
        this.sortOrder = sortOrder;
    }

    public String getItemSlug() { return itemSlug; }
    public String getItemName() { return itemName; }
    public String getItemIconUrl() { return itemIconUrl; }
    public Integer getQuantityMin() { return quantityMin; }
    public Integer getQuantityMax() { return quantityMax; }
    public BigDecimal getProbabilityPercent() { return probabilityPercent; }
    public String getLevelLabel() { return levelLabel; }
    public int getSortOrder() { return sortOrder; }
}
