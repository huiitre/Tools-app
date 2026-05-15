package fr.huiitre.tools.modules.riot.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinChromaSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinDataProvider;
import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinLevelSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinSyncData;

public class ValorantLocalSkinDataProvider implements ValorantSkinDataProvider {

    private final ValorantLocalAssetsReader assetsReader;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public ValorantLocalSkinDataProvider(ValorantLocalAssetsReader assetsReader, String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    @Override
    public List<ValorantSkinSyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("weapons.json");
            JsonNode root = objectMapper.readTree(json);
            JsonNode data = root.path("data");

            List<ValorantSkinSyncData> result = new ArrayList<>();

            for (JsonNode weapon : data) {
                UUID weaponAssetId = UUID.fromString(weapon.get("uuid").asText());
                for (JsonNode skin : weapon.path("skins")) {
                    UUID assetId = UUID.fromString(skin.get("uuid").asText());
                    String name = skin.path("displayName").asText(null);
                    String iconUrl = resolveSkinIconUrl(assetId, skin);
                    UUID tierUuid = parseUuid(skin.path("themeUuid").asText(null));
                    UUID contentTierUuid = parseUuid(skin.path("contentTierUuid").asText(null));
                    List<ValorantSkinLevelSyncData> levels = parseLevels(skin.path("levels"));
                    List<ValorantSkinChromaSyncData> chromas = parseChromas(skin.path("chromas"));

                    result.add(new ValorantSkinSyncData(assetId, name, iconUrl, tierUuid, contentTierUuid, weaponAssetId, levels, chromas));
                }
            }

            return result;

        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Valorant skins from local assets", e);
        }
    }

    private List<ValorantSkinLevelSyncData> parseLevels(JsonNode levelsNode) {
        List<ValorantSkinLevelSyncData> levels = new ArrayList<>();
        if (!levelsNode.isArray()) return levels;

        for (int i = 0; i < levelsNode.size(); i++) {
            JsonNode level = levelsNode.get(i);
            UUID levelAssetId = UUID.fromString(level.get("uuid").asText());
            String name = level.path("displayName").asText(null);
            String levelItem = level.path("levelItem").asText(null);
            String displayIconUrl = resolveLevelIconUrl(levelAssetId, level);
            String streamedVideoUrl = level.path("streamedVideo").asText(null);

            levels.add(new ValorantSkinLevelSyncData(levelAssetId, i, name, levelItem, displayIconUrl, streamedVideoUrl));
        }

        return levels;
    }

    private List<ValorantSkinChromaSyncData> parseChromas(JsonNode chromasNode) {
        List<ValorantSkinChromaSyncData> chromas = new ArrayList<>();
        if (!chromasNode.isArray()) return chromas;

        for (int i = 0; i < chromasNode.size(); i++) {
            JsonNode chroma = chromasNode.get(i);
            UUID chromaAssetId = UUID.fromString(chroma.get("uuid").asText());
            String name = chroma.path("displayName").asText(null);
            String displayIconUrl = resolveCdnUrl(chroma.path("displayIcon").asText(null),
                    "/tools_riot/valorant/img/weaponskinchromas/" + chromaAssetId + "/displayicon.png");
            String fullRenderUrl = resolveCdnUrl(chroma.path("fullRender").asText(null),
                    "/tools_riot/valorant/img/weaponskinchromas/" + chromaAssetId + "/fullrender.png");
            String swatchUrl = resolveCdnUrl(chroma.path("swatch").asText(null),
                    "/tools_riot/valorant/img/weaponskinchromas/" + chromaAssetId + "/swatch.png");
            String streamedVideoUrl = chroma.path("streamedVideo").asText(null);

            chromas.add(new ValorantSkinChromaSyncData(chromaAssetId, i, name, displayIconUrl, fullRenderUrl, swatchUrl, streamedVideoUrl));
        }

        return chromas;
    }

    private String resolveCdnUrl(String cdnValue, String localPath) {
        return (cdnValue != null && !cdnValue.isBlank()) ? assetsBaseUrl + localPath : null;
    }

    private String resolveSkinIconUrl(UUID assetId, JsonNode skin) {
        String displayIcon = skin.path("displayIcon").asText(null);
        if (displayIcon != null && !displayIcon.isBlank()) {
            return assetsBaseUrl + "/tools_riot/valorant/img/weaponskins/" + assetId + "/displayicon.png";
        }

        JsonNode levels = skin.path("levels");
        if (levels.isArray()) {
            for (JsonNode level : levels) {
                String levelIcon = level.path("displayIcon").asText(null);
                if (levelIcon != null && !levelIcon.isBlank()) {
                    UUID levelAssetId = UUID.fromString(level.get("uuid").asText());
                    return assetsBaseUrl + "/tools_riot/valorant/img/weaponskinlevels/" + levelAssetId + "/displayicon.png";
                }
            }
        }

        return null;
    }

    private String resolveLevelIconUrl(UUID levelAssetId, JsonNode level) {
        return resolveCdnUrl(level.path("displayIcon").asText(null),
                "/tools_riot/valorant/img/weaponskinlevels/" + levelAssetId + "/displayicon.png");
    }

    private UUID parseUuid(String value) {
        return (value != null && !value.isBlank()) ? UUID.fromString(value) : null;
    }
}
