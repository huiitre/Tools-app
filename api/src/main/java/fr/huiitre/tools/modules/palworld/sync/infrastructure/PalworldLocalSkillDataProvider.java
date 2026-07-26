package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.SkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.SkillDataProvider;

public class PalworldLocalSkillDataProvider implements SkillDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalSkillDataProvider(PalworldLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public List<SkillSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("skills.json"));
            OffsetDateTime fetchedAt = assetsReader.readScrapedAt();

            List<SkillSyncData> result = new ArrayList<>();
            for (JsonNode skill : root) {
                String slug = skill.path("slug").asText(null);
                String category = skill.path("category").asText(null);
                String name = skill.path("name").asText(null);
                String iconUrl = skill.path("icon").asText(null);
                String elementExternalCode = firstElementCode(skill.path("elements"));
                BigDecimal cooldown = parseDecimal(skill.path("cooldown").asText(null));
                Integer power = parseInt(skill.path("power").asText(null));
                String statusEffect = skill.path("statusEffect").asText(null);
                String description = skill.path("description").asText(null);

                result.add(new SkillSyncData(slug, category, name, iconUrl, elementExternalCode, cooldown, power,
                        statusEffect, description, null, skill.toString(), fetchedAt));
            }

            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld skills from local assets", e);
        }
    }

    private String firstElementCode(JsonNode elements) {
        if (elements == null || !elements.isArray() || elements.isEmpty()) {
            return null;
        }
        return elements.get(0).path("id").asText(null);
    }

    private BigDecimal parseDecimal(String raw) {
        if (raw == null || raw.isBlank()) return null;
        try {
            return new BigDecimal(raw.trim());
        } catch (NumberFormatException e) {
            return null;
        }
    }

    private Integer parseInt(String raw) {
        if (raw == null || raw.isBlank()) return null;
        try {
            return Integer.valueOf(raw.trim());
        } catch (NumberFormatException e) {
            return null;
        }
    }
}
