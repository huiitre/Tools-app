package fr.huiitre.tools.modules.palworld.sync.application;

import java.math.BigDecimal;
import java.time.OffsetDateTime;

public class SkillSyncData {

    private final String slug;
    private final String category;
    private final String name;
    private final String iconUrl;
    private final String elementExternalCode;
    private final BigDecimal cooldown;
    private final Integer power;
    private final String statusEffect;
    private final String description;
    private final String sourceUrl;
    private final String rawPayloadJson;
    private final OffsetDateTime fetchedAt;

    public SkillSyncData(String slug, String category, String name, String iconUrl, String elementExternalCode,
            BigDecimal cooldown, Integer power, String statusEffect, String description, String sourceUrl,
            String rawPayloadJson, OffsetDateTime fetchedAt) {
        this.slug = slug;
        this.category = category;
        this.name = name;
        this.iconUrl = iconUrl;
        this.elementExternalCode = elementExternalCode;
        this.cooldown = cooldown;
        this.power = power;
        this.statusEffect = statusEffect;
        this.description = description;
        this.sourceUrl = sourceUrl;
        this.rawPayloadJson = rawPayloadJson;
        this.fetchedAt = fetchedAt;
    }

    public String getSlug() { return slug; }
    public String getCategory() { return category; }
    public String getName() { return name; }
    public String getIconUrl() { return iconUrl; }
    public String getElementExternalCode() { return elementExternalCode; }
    public BigDecimal getCooldown() { return cooldown; }
    public Integer getPower() { return power; }
    public String getStatusEffect() { return statusEffect; }
    public String getDescription() { return description; }
    public String getSourceUrl() { return sourceUrl; }
    public String getRawPayloadJson() { return rawPayloadJson; }
    public OffsetDateTime getFetchedAt() { return fetchedAt; }
}
