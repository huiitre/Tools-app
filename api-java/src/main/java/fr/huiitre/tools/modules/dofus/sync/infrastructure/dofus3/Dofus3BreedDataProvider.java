package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.breed.domain.BreedSex;
import fr.huiitre.tools.modules.dofus.sync.application.breed.BreedDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.breed.BreedSyncData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

public class Dofus3BreedDataProvider implements BreedDataProvider {

    private final Dofus3LocalAssetsReader assetsReader;
    private final Dofus3LanguageDataProvider languageDataProvider;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public Dofus3BreedDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
    }

    //* Têtes exportées en img/class_head/Head_{breedId}{0 homme | 1 femme} : sans rapport avec maleLook/femaleLook.
    static Long headIconId(long breedId, BreedSex sex) {
        return breedId * 10 + (sex == BreedSex.MALE ? 0 : 1);
    }

    @Override
    public List<BreedSyncData> fetchAll() {

        try {

            JsonNode refIds = objectMapper.readTree(assetsReader.readFile("breeds.json"))
                    .path("references")
                    .path("RefIds");

            List<BreedSyncData> result = new ArrayList<>();

            for (JsonNode ref : refIds) {

                if (!"BreedData".equals(ref.path("type").path("class").asText())) {
                    continue;
                }

                JsonNode data = ref.path("data");
                long assetId = data.path("id").asLong();

                result.add(new BreedSyncData(
                    assetId,
                    languageDataProvider.getString(data.path("shortNameId").asLong()),
                    Map.of(
                        BreedSex.MALE, headIconId(assetId, BreedSex.MALE),
                        BreedSex.FEMALE, headIconId(assetId, BreedSex.FEMALE))
                ));
            }

            return result;

        } catch (Exception e) {
            throw new IllegalStateException(
                "Failed to load Dofus3 breed file (breeds.json)",
                e);
        }
    }
}
