package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.PassiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PassiveSkillDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalworldLanguageDataProvider;

public class PalworldLocalPassiveSkillDataProvider implements PassiveSkillDataProvider {

    private static final String DISPLAYABLE_CATEGORY = "EPalPassiveCategory::SortDisplayable";

    private final PalworldLocalAssetsReader assetsReader;
    private final PalworldLanguageDataProvider languageDataProvider;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalPassiveSkillDataProvider(
            PalworldLocalAssetsReader assetsReader,
            PalworldLanguageDataProvider languageDataProvider,
            String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    @Override
    public List<PassiveSkillSyncData> fetchDisplayable() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("passive_skills.json"));
            Map<String, String> rankImages = assetsReader.preferredImageFileNameByBaseName("passiveSkillRank");
            List<PassiveSkillSyncData> result = new ArrayList<>();

            for (JsonNode passiveSkill : root) {
                JsonNode raw = passiveSkill.path("raw");
                if (!DISPLAYABLE_CATEGORY.equals(raw.path("Category").asText())) continue;

                String id = passiveSkill.path("id").asText(null);
                if (id == null || id.isBlank()) continue;

                int rank = passiveSkill.path("rank").asInt();
                result.add(new PassiveSkillSyncData(
                        id,
                        languageDataProvider.getString(passiveSkill.path("name").asText(null)),
                        languageDataProvider.getDescription(passiveSkill.path("description").asText(null)),
                        rank,
                        rank < 0,
                        raw.path("AddWorldTreePal").asBoolean(false),
                        resolveRankIconUrl(rank, rankImages),
                        objectMapper.writeValueAsString(raw)));
            }

            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load displayable Palworld passive skills from local assets", e);
        }
    }

    private String resolveRankIconUrl(int rank, Map<String, String> rankImages) {
        String rankIconKey = rank == 5 ? "04" : String.format("%02d", rank);
        String fileName = rankImages.get(rankIconKey);
        return fileName == null ? null : assetsBaseUrl + "/tools_palworld/palworld/img/passiveSkillRank/" + fileName;
    }
}
