package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.SkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalworldLanguageDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.SkillDataProvider;

public class PalworldLocalSkillDataProvider implements SkillDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final PalworldLanguageDataProvider languageDataProvider;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalSkillDataProvider(
            PalworldLocalAssetsReader assetsReader, PalworldLanguageDataProvider languageDataProvider) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
    }

    @Override
    public List<SkillSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("skills.json"));
            OffsetDateTime fetchedAt = assetsReader.readScrapedAt();

            List<SkillSyncData> result = new ArrayList<>();
            for (JsonNode skill : root) {
                // "id" (ex: "AquaJet") remplace l'ancien "slug" scrapé (ex: "Aqua_Jet") — plus aucun fichier
                // d'img/activeSkill/ ne correspond à ce nouveau format, l'icône n'est plus résolue (accepté,
                // les compétences actives n'ont pas besoin d'icône, seulement des stats de combat).
                String slug = skill.path("id").asText(null);
                String category = skill.path("category").asText(null);

                String nameStringId = skill.path("name").isNull() ? null : skill.path("name").asText(null);
                // Certaines compétences (environnementales/cachées) n'ont pas de nom traduit du tout —
                // on retombe sur l'id brut du pak plutôt que de violer la contrainte NOT NULL de skill.name.
                String name = nameStringId != null ? languageDataProvider.getString(nameStringId) : slug;

                String descriptionStringId = skill.path("description").isNull() ? null : skill.path("description").asText(null);
                String description = descriptionStringId != null ? languageDataProvider.getDescription(descriptionStringId) : null;

                // Valeur brute EPalElementType (ex: "Earth") — même vocabulaire que pal.elementTypes[],
                // jointe via elements.json[].palElementType, cf. SyncElementsUseCase.idByPalElementType().
                String elementExternalCode = skill.path("element").asText(null);
                BigDecimal cooldown = decimalOrNull(skill.path("coolTime"));
                Integer power = intOrNull(skill.path("power"));
                // "statusEffect" a disparu du pak, remplacé par "strength" (Weak/Medium/...) qui n'a pas le
                // même sens (catégorie de puissance, pas un effet de statut) — non mappé tant que non confirmé.
                String statusEffect = null;

                result.add(new SkillSyncData(slug, category, name, null, elementExternalCode, cooldown, power,
                        statusEffect, description, null, skill.toString(), fetchedAt));
            }

            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld skills from local assets", e);
        }
    }

    private Integer intOrNull(JsonNode node) {
        return node.isMissingNode() || node.isNull() ? null : node.asInt();
    }

    private BigDecimal decimalOrNull(JsonNode node) {
        return node.isMissingNode() || node.isNull() ? null : node.decimalValue();
    }
}
