package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.PalActiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalDropSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalElementSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalPartnerSkillRankSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalPartnerSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalPassiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalSpawnZoneSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalVariantSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalWorkSuitabilitySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalDataProvider;

public class PalworldLocalPalDataProvider implements PalDataProvider {

    private static final Pattern PROBABILITY_WITH_LEVEL = Pattern.compile("^(Lv\\.?\\s*\\d+)\\s+([\\d.]+)%$");
    private static final Pattern PROBABILITY_PLAIN = Pattern.compile("^([\\d.]+)%$");

    private final PalworldLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalPalDataProvider(PalworldLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public List<PalSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("pals.json"));
            OffsetDateTime fetchedAt = assetsReader.readScrapedAt();

            List<PalSyncData> result = new ArrayList<>();
            for (JsonNode pal : root) {
                result.add(toSyncData(pal, fetchedAt));
            }
            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld pals from local assets", e);
        }
    }

    private PalSyncData toSyncData(JsonNode pal, OffsetDateTime fetchedAt) {
        JsonNode stats = pal.path("raw").path("Stats");
        JsonNode others = pal.path("raw").path("Others");
        JsonNode movement = pal.path("raw").path("Movement");

        return new PalSyncData(
                pal.path("tribe").asText(null),
                parseInt(pal.path("paldexIndex").asText(null)),
                pal.path("suffix").isNull() ? null : pal.path("suffix").asText(null),
                pal.path("name").asText(null),
                pal.path("image").asText(null),
                pal.path("description").asText(null),
                statText(stats, "Size"),
                parseInt(statText(stats, "Rarity")),
                parseInt(statText(stats, "PV")),
                parseInt(statText(stats, "Attaque")),
                parseInt(statText(stats, "Défense")),
                parseInt(statText(stats, "Vitesse de travail")),
                parseInt(statText(stats, "Support")),
                parseInt(statText(stats, "Quantité de nourriture")),
                parseInt(movement.path("RunSpeed").asText(null)),
                parseInt(movement.path("RideSprintSpeed").asText(null)),
                parseDecimal(statText(stats, "CaptureRateCorrect")),
                parseDecimal(statText(stats, "MaleProbability")),
                parseInt(statText(stats, "CombiRank")),
                parseInt(statText(stats, "Pièce d'or")),
                statText(stats, "Egg"),
                statText(others, "BestWorkSuitability"),
                elements(pal.path("elements")),
                workSuitabilities(pal.path("workSuitability")),
                activeSkills(pal.path("activeSkills")),
                passiveSkills(pal.path("passiveSkills")),
                partnerSkill(pal.path("partnerSkill")),
                drops(pal.path("drops")),
                variants(pal.path("tribeVariants")),
                spawnZones(pal.path("spawner")),
                pal.path("slug").asText(null),
                null,
                pal.toString(),
                fetchedAt);
    }

    private String statText(JsonNode block, String key) {
        return block.path(key).path("text").asText(null);
    }

    private List<PalElementSyncData> elements(JsonNode node) {
        List<PalElementSyncData> result = new ArrayList<>();
        int order = 0;
        for (JsonNode element : node) {
            result.add(new PalElementSyncData(element.path("id").asText(null), element.path("icon").asText(null), order++));
        }
        return result;
    }

    private List<PalWorkSuitabilitySyncData> workSuitabilities(JsonNode node) {
        List<PalWorkSuitabilitySyncData> result = new ArrayList<>();
        for (JsonNode ws : node) {
            Integer level = parseInt(ws.path("level").asText(null));
            result.add(new PalWorkSuitabilitySyncData(ws.path("slug").asText(null), ws.path("icon").asText(null), level != null ? level : 0));
        }
        return result;
    }

    private List<PalActiveSkillSyncData> activeSkills(JsonNode node) {
        List<PalActiveSkillSyncData> result = new ArrayList<>();
        int order = 0;
        for (JsonNode skill : node) {
            Integer level = parseInt(skill.path("level").asText(null));
            result.add(new PalActiveSkillSyncData(skill.path("link").asText(null), level != null ? level : 0, order++));
        }
        return result;
    }

    private List<PalPassiveSkillSyncData> passiveSkills(JsonNode node) {
        List<PalPassiveSkillSyncData> result = new ArrayList<>();
        for (JsonNode passive : node) {
            String name = passive.path("name").asText(null);
            String tooltip = passive.path("tooltip").isNull() ? null : passive.path("tooltip").asText(null);
            String rankIconUrl = passive.path("rankIcon").asText(null);

            boolean alreadyPresent = result.stream()
                    .anyMatch(p -> java.util.Objects.equals(p.getName(), name) && java.util.Objects.equals(p.getTooltip(), tooltip));
            if (!alreadyPresent) {
                result.add(new PalPassiveSkillSyncData(name, tooltip, rankIconUrl));
            }
        }
        return result;
    }

    private PalPartnerSkillSyncData partnerSkill(JsonNode node) {
        if (node.isMissingNode() || node.isNull()) {
            return null;
        }

        List<PalPartnerSkillRankSyncData> ranks = new ArrayList<>();
        int order = 1;
        for (JsonNode rank : node.path("ranks")) {
            ranks.add(new PalPartnerSkillRankSyncData(order++, rank.path("level").asText(null), rank.path("detail").asText(null)));
        }

        return new PalPartnerSkillSyncData(
                node.path("title").asText(null),
                node.path("description").asText(null),
                node.path("icon").asText(null),
                ranks);
    }

    private List<PalDropSyncData> drops(JsonNode node) {
        List<PalDropSyncData> result = new ArrayList<>();
        int order = 0;
        for (JsonNode drop : node) {
            int[] quantity = parseQuantity(drop.path("quantity").asText(null));
            String probabilityRaw = drop.path("probability").asText(null);

            result.add(new PalDropSyncData(
                    drop.path("item").asText(null),
                    drop.path("itemName").asText(null),
                    drop.path("icon").asText(null),
                    quantity[0] >= 0 ? quantity[0] : null,
                    quantity[1] >= 0 ? quantity[1] : null,
                    parseProbabilityPercent(probabilityRaw),
                    parseProbabilityLevelLabel(probabilityRaw),
                    order++));
        }
        return result;
    }

    private List<PalVariantSyncData> variants(JsonNode node) {
        List<PalVariantSyncData> result = new ArrayList<>();
        int order = 0;
        for (JsonNode variant : node) {
            result.add(new PalVariantSyncData(
                    variant.path("slug").asText(null),
                    variant.path("name").asText(null),
                    variant.path("icon").asText(null),
                    variant.path("role").asText(null),
                    order++));
        }
        return result;
    }

    private List<PalSpawnZoneSyncData> spawnZones(JsonNode node) {
        List<PalSpawnZoneSyncData> result = new ArrayList<>();
        int order = 0;
        for (JsonNode zone : node) {
            result.add(new PalSpawnZoneSyncData(
                    zone.path("level").asText(null),
                    zone.path("location").asText(null),
                    zone.path("locationLink").isNull() ? null : zone.path("locationLink").asText(null),
                    order++));
        }
        return result;
    }

    private int[] parseQuantity(String raw) {
        if (raw == null || raw.isBlank()) {
            return new int[] { -1, -1 };
        }
        String[] parts = raw.split("[–-]");
        try {
            if (parts.length == 2) {
                return new int[] { Integer.parseInt(parts[0].trim()), Integer.parseInt(parts[1].trim()) };
            }
            int value = Integer.parseInt(raw.trim());
            return new int[] { value, value };
        } catch (NumberFormatException e) {
            return new int[] { -1, -1 };
        }
    }

    private BigDecimal parseProbabilityPercent(String raw) {
        if (raw == null || raw.isBlank()) return null;

        Matcher withLevel = PROBABILITY_WITH_LEVEL.matcher(raw.trim());
        if (withLevel.matches()) {
            return new BigDecimal(withLevel.group(2));
        }

        Matcher plain = PROBABILITY_PLAIN.matcher(raw.trim());
        if (plain.matches()) {
            return new BigDecimal(plain.group(1));
        }

        return null;
    }

    private String parseProbabilityLevelLabel(String raw) {
        if (raw == null || raw.isBlank()) return null;

        Matcher withLevel = PROBABILITY_WITH_LEVEL.matcher(raw.trim());
        return withLevel.matches() ? withLevel.group(1) : null;
    }

    private Integer parseInt(String raw) {
        if (raw == null || raw.isBlank()) return null;
        try {
            return Integer.valueOf(raw.trim());
        } catch (NumberFormatException e) {
            return null;
        }
    }

    private BigDecimal parseDecimal(String raw) {
        if (raw == null || raw.isBlank()) return null;
        try {
            return new BigDecimal(raw.trim());
        } catch (NumberFormatException e) {
            return null;
        }
    }
}
