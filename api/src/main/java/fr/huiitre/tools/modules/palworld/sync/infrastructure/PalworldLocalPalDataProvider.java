package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.Objects;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.PalActiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalDropSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalElementSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalPassiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalWorkSuitabilitySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalDataProvider;

public class PalworldLocalPalDataProvider implements PalDataProvider {

    // Le pak expose les valeurs internes du jeu (enum EPalElementType / EPalWorkSuitability), alors que
    // elements.json/work_suitability.json (scraper paldb.cc, inchangés) utilisent leur propre vocabulaire
    // anglais. Ce ne sont ni les mêmes casse ni les mêmes mots : table de correspondance figée, ces deux
    // catalogues sont petits et stables entre mises à jour du jeu.
    private static final Map<String, String> ELEMENT_NAME_BY_PAK_TYPE = Map.of(
            "Fire", "Fire",
            "Water", "Water",
            "Electricity", "Electric",
            "Leaf", "Grass",
            "Dark", "Dark",
            "Dragon", "Dragon",
            "Earth", "Ground",
            "Ice", "Ice",
            "Normal", "Neutral");

    private static final Map<String, String> WORK_SUITABILITY_SLUG_BY_PAK_CATEGORY = Map.ofEntries(
            Map.entry("EmitFlame", "Kindling"),
            Map.entry("Watering", "Watering"),
            Map.entry("Seeding", "Planting"),
            Map.entry("GenerateElectricity", "Generating_Electricity"),
            Map.entry("Handcraft", "Handiwork"),
            Map.entry("Collection", "Gathering"),
            Map.entry("Deforest", "Lumbering"),
            Map.entry("Mining", "Mining"),
            Map.entry("ProductMedicine", "Medicine_Production"),
            Map.entry("Cool", "Cooling"),
            Map.entry("Transport", "Transporting"),
            Map.entry("MonsterFarm", "Farming"));
    // "OilExtraction" (pak) n'a aucun équivalent dans work_suitability.json — absent du catalogue scrapé
    // (paldb.cc ne le référence pas). Aucun Pal du jeu n'a ce niveau > 0 à ce jour, donc sans impact visible.

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
        JsonNode stats = pal.path("stats");
        JsonNode movement = pal.path("movement");
        String tribe = pal.path("id").asText(null);

        List<PalWorkSuitabilitySyncData> workSuitabilities = workSuitabilities(pal.path("workSuitability"));

        return new PalSyncData(
                tribe,
                intOrNull(pal.path("paldexIndex")),
                pal.path("name").asText(null),
<<<<<<< Updated upstream
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
                parseInt(statText(movement, "RunSpeed")),
                parseInt(statText(movement, "RideSprintSpeed")),
                parseDecimal(statText(stats, "CaptureRateCorrect")),
                parseDecimal(statText(stats, "MaleProbability")),
                parseInt(statText(stats, "CombiRank")),
                parseInt(statText(stats, "Pièce d'or")),
                statText(stats, "Egg"),
                statText(others, "BestWorkSuitability"),
                parseInt(pal.path("foodAmount").path("on").asText(null)),
                parseInt(pal.path("foodAmount").path("off").asText(null)),
                pal.path("foodAmount").path("icon").asText(null),
                elements(pal.path("elements")),
                workSuitabilities(pal.path("workSuitability")),
=======
                pal.path("size").asText(null),
                intOrNull(pal.path("rarity")),
                intOrNull(stats.path("hp")),
                intOrNull(stats.path("meleeAttack")),
                intOrNull(stats.path("defense")),
                intOrNull(stats.path("craftSpeed")),
                intOrNull(stats.path("support")),
                intOrNull(movement.path("run")),
                intOrNull(movement.path("rideSprint")),
                decimalOrNull(pal.path("captureRateCorrect")),
                decimalOrNull(pal.path("maleProbability")),
                intOrNull(pal.path("combiRank")),
                intOrNull(pal.path("price")),
                bestWorkSuitability(workSuitabilities),
                elements(pal.path("elementTypes")),
                workSuitabilities,
>>>>>>> Stashed changes
                activeSkills(pal.path("activeSkills")),
                passiveSkills(pal.path("passiveSkills")),
                drops(pal.path("drops")),
                tribe,
                null,
                pal.toString(),
                fetchedAt);
    }

    private List<PalElementSyncData> elements(JsonNode node) {
        List<PalElementSyncData> result = new ArrayList<>();
        int order = 0;
        for (JsonNode element : node) {
            String pakType = element.asText(null);
            String elementName = ELEMENT_NAME_BY_PAK_TYPE.getOrDefault(pakType, pakType);
            result.add(new PalElementSyncData(elementName, null, order++));
        }
        return result;
    }

    private List<PalWorkSuitabilitySyncData> workSuitabilities(JsonNode node) {
        List<PalWorkSuitabilitySyncData> result = new ArrayList<>();
        for (JsonNode ws : node) {
<<<<<<< Updated upstream
            Integer level = parseInt(ws.path("level").asText(null));
            Integer maxLevel = parseInt(ws.path("maxLevel").asText(null));
            Integer starSegments = parseInt(ws.path("starSegments").asText(null));
            Integer emptySegments = parseInt(ws.path("emptySegments").asText(null));
            boolean isPriority = ws.path("isPriority").asBoolean(false);
            result.add(new PalWorkSuitabilitySyncData(ws.path("slug").asText(null), ws.path("icon").asText(null),
                    level != null ? level : 0, maxLevel, starSegments, emptySegments, isPriority));
=======
            int level = ws.path("level").asInt(0);
            // Le pak liste les 13 catégories pour chaque Pal (0 pour celles qu'il n'a pas) — l'ancien scraper
            // ne remontait que les aptitudes réellement présentes (~2,5 en moyenne par Pal contre 13 sinon).
            if (level <= 0) continue;

            String pakCategory = ws.path("category").asText(null);
            String slug = WORK_SUITABILITY_SLUG_BY_PAK_CATEGORY.getOrDefault(pakCategory, pakCategory);
            result.add(new PalWorkSuitabilitySyncData(slug, null, level));
>>>>>>> Stashed changes
        }
        return result;
    }

    private String bestWorkSuitability(List<PalWorkSuitabilitySyncData> workSuitabilities) {
        return workSuitabilities.stream()
                .max(Comparator.comparingInt(PalWorkSuitabilitySyncData::getLevel))
                .map(PalWorkSuitabilitySyncData::getSlug)
                .orElse(null);
    }

    private List<PalActiveSkillSyncData> activeSkills(JsonNode node) {
        List<PalActiveSkillSyncData> result = new ArrayList<>();
        int order = 0;
        for (JsonNode skill : node) {
            // Jointure par nom (FR) et non par id : le "id" interne du pak (ex: "PoisonFog") ne correspond pas
            // au slug scrapé de skills.json (ex: "Poison_Fog" est en fait un slug propre à paldb.cc, sans lien
            // mécanique avec le nom interne) — en revanche le nom affiché est bien le même texte des deux côtés.
            result.add(new PalActiveSkillSyncData(skill.path("name").asText(null), skill.path("level").asInt(0), order++));
        }
        return result;
    }

    private List<PalPassiveSkillSyncData> passiveSkills(JsonNode node) {
        List<PalPassiveSkillSyncData> result = new ArrayList<>();
        for (JsonNode passive : node) {
            String name = passive.path("name").asText(null);
            boolean alreadyPresent = result.stream().anyMatch(p -> Objects.equals(p.getName(), name));
            if (!alreadyPresent) {
                result.add(new PalPassiveSkillSyncData(name, null, null));
            }
        }
        return result;
    }

    private List<PalDropSyncData> drops(JsonNode node) {
        List<PalDropSyncData> result = new ArrayList<>();
        int order = 0;
        for (JsonNode levelDrop : node) {
            String levelLabel = levelDrop.path("level").asText(null);
            for (JsonNode item : levelDrop.path("items")) {
                String itemId = item.path("itemId").asText(null);
                result.add(new PalDropSyncData(
                        itemId,
                        itemId,
                        null,
                        intOrNull(item.path("min")),
                        intOrNull(item.path("max")),
                        decimalOrNull(item.path("rate")),
                        levelLabel,
                        order++));
            }
        }
        return result;
    }

    private Integer intOrNull(JsonNode node) {
        return node.isMissingNode() || node.isNull() ? null : node.asInt();
    }

    private BigDecimal decimalOrNull(JsonNode node) {
        return node.isMissingNode() || node.isNull() ? null : node.decimalValue();
    }
}
