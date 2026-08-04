package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.stream.Collectors;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.PalActiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalDropSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalElementSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalPassiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalWorkSuitabilitySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalworldLanguageDataProvider;

public class PalworldLocalPalDataProvider implements PalDataProvider {

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
    private final PalworldLanguageDataProvider languageDataProvider;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalPalDataProvider(
            PalworldLocalAssetsReader assetsReader, PalworldLanguageDataProvider languageDataProvider, String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    @Override
    public List<PalSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("pals.json"));
            OffsetDateTime fetchedAt = assetsReader.readScrapedAt();

            // Le pak ne référence aucune image : le rip d'assets img/pal/{tribe}.webp vit sur le NAS,
            // indépendamment du pak, avec quelques divergences de casse (BluePlatypus -> Blueplatypus.webp)
            // et des Pals très récents pas encore rippés (cf. matching insensible à la casse ci-dessous).
            Map<String, String> palImageFileNameByTribeUpper = assetsReader.listImageFileNames("pal").stream()
                    .collect(Collectors.toMap(
                            fileName -> stripExtension(fileName).toUpperCase(),
                            fileName -> fileName,
                            (a, b) -> a));

            List<PalSyncData> result = new ArrayList<>();
            for (JsonNode pal : root) {
                result.add(toSyncData(pal, fetchedAt, palImageFileNameByTribeUpper));
            }
            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld pals from local assets", e);
        }
    }

    private String stripExtension(String fileName) {
        int dot = fileName.lastIndexOf('.');
        return dot > 0 ? fileName.substring(0, dot) : fileName;
    }

    private String resolveImageUrl(String tribe, Map<String, String> palImageFileNameByTribeUpper) {
        String fileName = palImageFileNameByTribeUpper.get(tribe.toUpperCase());
        return fileName != null ? assetsBaseUrl + "/tools_palworld/palworld/img/pal/" + fileName : null;
    }

    private PalSyncData toSyncData(JsonNode pal, OffsetDateTime fetchedAt, Map<String, String> palImageFileNameByTribeUpper) {
        JsonNode stats = pal.path("stats");
        JsonNode movement = pal.path("movement");
        String tribe = pal.path("id").asText(null);

        List<PalWorkSuitabilitySyncData> workSuitabilities = workSuitabilities(pal.path("workSuitability"));

        return new PalSyncData(
                tribe,
                intOrNull(pal.path("paldexIndex")),
                languageDataProvider.getString(pal.path("name").asText(null)),
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
                resolveImageUrl(tribe, palImageFileNameByTribeUpper),
                languageDataProvider.getDescription(pal.path("description").asText(null)),
                null, null, null,
                elements(pal.path("elementTypes")),
                workSuitabilities,
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
            // Valeur brute EPalElementType (ex: "Leaf") — jointe via elements.json[].palElementType,
            // cf. SyncElementsUseCase.idByPalElementType(). Pas de vocabulaire à traduire côté Java.
            result.add(new PalElementSyncData(element.asText(null), null, order++));
        }
        return result;
    }

    private List<PalWorkSuitabilitySyncData> workSuitabilities(JsonNode node) {
        List<PalWorkSuitabilitySyncData> result = new ArrayList<>();
        for (JsonNode ws : node) {
            int level = ws.path("level").asInt(0);
            // Le pak liste les 13 catégories pour chaque Pal (0 pour celles qu'il n'a pas) — l'ancien scraper
            // ne remontait que les aptitudes réellement présentes (~2,5 en moyenne par Pal contre 13 sinon).
            if (level <= 0) continue;

            String pakCategory = ws.path("category").asText(null);
            String slug = WORK_SUITABILITY_SLUG_BY_PAK_CATEGORY.getOrDefault(pakCategory, pakCategory);
            // maxLevel/starSegments/emptySegments/isPriority : pas de donnée pak, cf. note dans
            // PostgresPalSyncRepository.saveWorkSuitabilities().
            result.add(new PalWorkSuitabilitySyncData(slug, null, level, null, null, null, false));
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
            // Jointure par "id" (ex: "PoisonFog") : correspond exactement à skill.id de skills.json,
            // stocké dans tools_palworld.skill.slug (cf. PalworldLocalSkillDataProvider).
            result.add(new PalActiveSkillSyncData(skill.path("id").asText(null), skill.path("level").asInt(0), order++));
        }
        return result;
    }

    private List<PalPassiveSkillSyncData> passiveSkills(JsonNode node) {
        List<PalPassiveSkillSyncData> result = new ArrayList<>();
        for (JsonNode passive : node) {
            String name = languageDataProvider.getString(passive.path("name").asText(null));
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
