package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.job.JobDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.job.JobSyncData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

public class Dofus3JobDataProvider implements JobDataProvider {

    //* iconId du métier « Base » (id 1), technique : pas un vrai métier.
    private static final long NO_ICON = -1;

    private final Dofus3LocalAssetsReader assetsReader;
    private final Dofus3LanguageDataProvider languageDataProvider;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public Dofus3JobDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
    }

    @Override
    public List<JobSyncData> fetchAll() {

        try {

            JsonNode refIds = objectMapper.readTree(assetsReader.readFile("jobs.json"))
                    .path("references")
                    .path("RefIds");

            List<JobSyncData> result = new ArrayList<>();

            for (JsonNode ref : refIds) {

                if (!"JobData".equals(ref.path("type").path("class").asText())) {
                    continue;
                }

                JsonNode data = ref.path("data");
                long iconId = data.path("iconId").asLong();

                if (iconId == NO_ICON) {
                    continue;
                }

                result.add(new JobSyncData(
                    data.path("id").asLong(),
                    languageDataProvider.getString(data.path("nameId").asLong()),
                    iconId
                ));
            }

            return result;

        } catch (Exception e) {
            throw new IllegalStateException(
                "Failed to load Dofus3 job file (jobs.json)",
                e);
        }
    }
}
