package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.WorkSuitabilitySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalworldLanguageDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkSuitabilityDataProvider;

public class PalworldLocalWorkSuitabilityDataProvider implements WorkSuitabilityDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final PalworldLanguageDataProvider languageDataProvider;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalWorkSuitabilityDataProvider(
            PalworldLocalAssetsReader assetsReader, PalworldLanguageDataProvider languageDataProvider, String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    @Override
    public List<WorkSuitabilitySyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("work_suitability.json");
            JsonNode root = objectMapper.readTree(json);

            // work_suitability.json "id" (ex: "07") correspond exactement au nom de fichier rippé
            // img/workSuitability/07.webp, même convention que les éléments — le champ "icon" du JSON
            // (CDN paldb.cc) est ignoré, jamais utilisé.
            Map<String, String> imageFileNameByCode = assetsReader.listImageFileNames("workSuitability").stream()
                    .collect(Collectors.toMap(this::stripExtension, fileName -> fileName, (a, b) -> a));

            List<WorkSuitabilitySyncData> result = new ArrayList<>();
            for (JsonNode ws : root) {
                String externalCode = ws.path("id").asText(null);
                result.add(new WorkSuitabilitySyncData(
                        externalCode,
                        ws.path("slug").asText(null),
                        languageDataProvider.getString(ws.path("nameStringId").asText(null)),
                        resolveIconUrl(externalCode, imageFileNameByCode)));
            }

            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld work suitabilities from local assets", e);
        }
    }

    private String stripExtension(String fileName) {
        int dot = fileName.lastIndexOf('.');
        return dot > 0 ? fileName.substring(0, dot) : fileName;
    }

    private String resolveIconUrl(String externalCode, Map<String, String> imageFileNameByCode) {
        String fileName = imageFileNameByCode.get(externalCode);
        return fileName != null ? assetsBaseUrl + "/tools_palworld/palworld/img/workSuitability/" + fileName : null;
    }
}
