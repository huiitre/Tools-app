package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.ElementSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ElementDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalworldLanguageDataProvider;

public class PalworldLocalElementDataProvider implements ElementDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final PalworldLanguageDataProvider languageDataProvider;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalElementDataProvider(
            PalworldLocalAssetsReader assetsReader, PalworldLanguageDataProvider languageDataProvider, String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    @Override
    public List<ElementSyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("elements.json");
            JsonNode root = objectMapper.readTree(json);

            // elements.json "id" (ex: "05") correspond exactement au nom de fichier rippé img/element/05.webp,
            // pas de divergence de casse/nommage ici contrairement aux Pals (cf. PalworldLocalPalDataProvider).
            Map<String, String> imageFileNameByCode = assetsReader.listImageFileNames("element").stream()
                    .collect(Collectors.toMap(this::stripExtension, fileName -> fileName, (a, b) -> a));

            List<ElementSyncData> result = new ArrayList<>();
            for (JsonNode element : root) {
                String externalCode = element.path("id").asText(null);
                String code = element.path("code").asText(null);
                String palElementType = element.path("palElementType").asText(null);
                String name = languageDataProvider.getString(element.path("nameStringId").asText(null));
                result.add(new ElementSyncData(
                        externalCode, code, palElementType, name, resolveIconUrl(externalCode, imageFileNameByCode)));
            }

            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld elements from local assets", e);
        }
    }

    private String stripExtension(String fileName) {
        int dot = fileName.lastIndexOf('.');
        return dot > 0 ? fileName.substring(0, dot) : fileName;
    }

    private String resolveIconUrl(String externalCode, Map<String, String> imageFileNameByCode) {
        String fileName = imageFileNameByCode.get(externalCode);
        return fileName != null ? assetsBaseUrl + "/tools_palworld/palworld/img/element/" + fileName : null;
    }
}
