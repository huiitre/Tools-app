package fr.huiitre.tools.modules.dofus.catalogue.application.data;

import fr.huiitre.tools.modules.dofus.catalogue.application.dto.CatalogueColumnDto;

import java.util.List;

public final class CatalogueColumnsDefinition {

    private CatalogueColumnsDefinition() {
        // static only
    }

    public static List<CatalogueColumnDto> all() {
        return List.of(

            /* =========================
               IDENTIFIANTS
            ========================= */

            new CatalogueColumnDto(
                "id",
                "ID",
                "Identifiant interne de l'objet",
                false,
                true,
                true,
                70,     // size
                70,     // minSize
                90      // maxSize
            ),

            new CatalogueColumnDto(
                "asset_id",
                "Asset ID",
                "Identifiant asset Dofus",
                false,
                true,
                true,
                80,
                80,
                100
            ),

            /* =========================
               MÉTADONNÉES OBJET
            ========================= */

            new CatalogueColumnDto(
                "type",
                "Type",
                "Type d'objet",
                true,
                true,
                true,
                110,
                100,
                140
            ),

            new CatalogueColumnDto(
                "name",
                "Nom",
                "Nom de l'objet",
                true,
                false,
                true,
                220,
                160,
                300
            ),

            new CatalogueColumnDto(
                "level",
                "Niveau",
                "Niveau de l'objet",
                true,
                false,
                true,
                60,
                60,
                80
            ),

            new CatalogueColumnDto(
                "quantity",
                "Qté",
                "Quantité requise",
                true,
                false,
                false,
                50,
                40,
                60
            ),

            new CatalogueColumnDto(
                "description",
                "Description",
                "Description de l'objet",
                true,
                true,
                false,
                360,
                220,
                800
            ),

            /* =========================
               PRIX
            ========================= */

            new CatalogueColumnDto(
                "user_price",
                "Mon prix",
                "Prix défini par l'utilisateur",
                true,
                true,
                false,
                120,
                110,
                140
            ),

            new CatalogueColumnDto(
                "community_average_price",
                "Prix commu",
                "Prix moyen communautaire",
                true,
                true,
                false,
                80,
                70,
                90
            ),

            new CatalogueColumnDto(
                "last_updated_price",
                "Dernier prix",
                "Dernier prix enregistré",
                true,
                true,
                false,
                80,
                70,
                90
            ),

            /* =========================
               CRAFT
            ========================= */

            new CatalogueColumnDto(
                "craft_user_price",
                "Craft (moi)",
                "Coût de craft basé sur mes prix",
                true,
                true,
                false,
                90,
                80,
                100
            ),

            new CatalogueColumnDto(
                "craft_community_price",
                "Craft (commu)",
                "Coût de craft basé sur les prix communautaires",
                true,
                true,
                false,
                90,
                80,
                100
            ),

            new CatalogueColumnDto(
                "craft_last_price",
                "Craft (dernier)",
                "Coût de craft basé sur les derniers prix",
                true,
                true,
                false,
                90,
                80,
                100
            ),

            new CatalogueColumnDto(
                "craft_calculated_price",
                "Craft calculé",
                "Meilleur coût de craft disponible",
                true,
                true,
                false,
                90,
                80,
                100
            )
        );
    }
}
