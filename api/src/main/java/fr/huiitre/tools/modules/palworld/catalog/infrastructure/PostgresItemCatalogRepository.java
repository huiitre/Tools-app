package fr.huiitre.tools.modules.palworld.catalog.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.catalog.application.ports.ItemCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.view.ItemCatalogView;

public class PostgresItemCatalogRepository implements ItemCatalogRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresItemCatalogRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public List<ItemCatalogView> findAll() {
        // price > 0 : le jeu utilise 0 comme sentinelle "non échangeable contre de l'or" (déblocages de
        // compétence, reliques...), vérifié empiriquement (169/2466 items, tous de ce type) — pas un filtre
        // arbitraire, ça exclut des objets qui n'ont réellement pas de prix d'achat/vente en or.
        final String sql = """
                SELECT i.id, i.slug, i.name, i.icon_url, i.category, i.price,
                       EXISTS(SELECT 1 FROM tools_palworld.merchant_offer mo WHERE mo.item_id = i.id) AS sold_by_merchant
                FROM tools_palworld.item i
                WHERE i.price > 0
                ORDER BY i.category NULLS LAST, i.name
                """;
        return jdbcTemplate.query(sql, (rs, rowNum) -> new ItemCatalogView(
                rs.getLong("id"),
                rs.getString("slug"),
                rs.getString("name"),
                rs.getString("icon_url"),
                rs.getString("category"),
                (Integer) rs.getObject("price"),
                rs.getBoolean("sold_by_merchant")));
    }
}
