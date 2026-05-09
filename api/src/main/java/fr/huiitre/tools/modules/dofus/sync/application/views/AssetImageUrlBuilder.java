package fr.huiitre.tools.modules.dofus.sync.application.views;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

@Component
public class AssetImageUrlBuilder {

    private final String baseUrl;

    public AssetImageUrlBuilder(
            @Value("${app.assets.base-url}") String baseUrl) {
        this.baseUrl = baseUrl;
    }

    /**
     * @param category   dossier sous /img (item, monster, spell, etc.)
     * @param iconId     identifiant de l’asset
     * @param resolution X1 ou X2
     */
    public String build(
            String category,
            Long iconId,
            AssetResolution resolution,
            String gameVersionCode) {

        if ("retro".equals(gameVersionCode)) {
            // Remplace par le bon chemin exact de tes assets SVG
            return baseUrl + "/tools_dofus/retro/img/items/" + iconId + ".svg";
        }

        String scale = resolution.getFolder();
        int size = resolution.getSize();

        return baseUrl
                + "/tools_dofus/dofus3/img/"
                + category + "/"
                + scale + "/"
                + iconId + "-" + size + ".png";
    }
}