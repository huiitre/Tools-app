package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.List;
import java.util.Map;

import org.junit.jupiter.api.Test;

import fr.huiitre.tools.modules.dofus.breed.domain.BreedSex;
import fr.huiitre.tools.modules.dofus.sync.application.breed.BreedSyncData;
import fr.huiitre.tools.modules.dofus.sync.application.job.JobSyncData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

// Extraits réels de breeds.json et jobs.json (Dofus 3, 18/11/2026) : shortNameId est en texte, le reste en nombres.
class Dofus3BreedJobDataProviderTest {

    private static final Dofus3LanguageDataProvider LANGUAGE = new Dofus3LanguageDataProvider() {
        private final Map<Long, String> strings = Map.of(689L, "Crâ", 2736L, "Base", 2706L, "Bûcheron");

        @Override
        public String getString(Long stringId) {
            return strings.get(stringId);
        }

        @Override
        public void reload() {
        }
    };

    private static Dofus3LocalAssetsReader readerOf(String file, String json) {
        return new Dofus3LocalAssetsReader() {
            @Override
            public String readFile(String relativePath) {
                assertEquals(file, relativePath);
                return json;
            }
        };
    }

    @Test
    void breedsAreReadWithBothHeads() {
        String json = """
            {"references": {"RefIds": [
              {"type": {"class": "BreedData"}, "data": {"id": 9, "shortNameId": "689", "maleLook": "{1|90||53}"}},
              {"type": {"class": "Other"}, "data": {"id": 99}}
            ]}}
            """;

        List<BreedSyncData> breeds = new Dofus3BreedDataProvider(readerOf("breeds.json", json), LANGUAGE).fetchAll();

        assertEquals(1, breeds.size());
        assertEquals(9L, breeds.get(0).assetId());
        assertEquals("Crâ", breeds.get(0).name());
        assertEquals(Map.of(BreedSex.MALE, 90L, BreedSex.FEMALE, 91L), breeds.get(0).headIconIdsBySex());
    }

    @Test
    void headIconIdIgnoresTheLook() {
        // Ouginak : look 3498/3499, mais têtes Head_180 / Head_181.
        assertEquals(180L, Dofus3BreedDataProvider.headIconId(18, BreedSex.MALE));
        assertEquals(181L, Dofus3BreedDataProvider.headIconId(18, BreedSex.FEMALE));
    }

    @Test
    void baseJobIsSkipped() {
        String json = """
            {"references": {"RefIds": [
              {"type": {"class": "JobData"}, "data": {"id": 1, "nameId": 2736, "iconId": -1}},
              {"type": {"class": "JobData"}, "data": {"id": 2, "nameId": 2706, "iconId": 1}}
            ]}}
            """;

        List<JobSyncData> jobs = new Dofus3JobDataProvider(readerOf("jobs.json", json), LANGUAGE).fetchAll();

        assertEquals(List.of(new JobSyncData(2L, "Bûcheron", 1L)), jobs);
    }
}
