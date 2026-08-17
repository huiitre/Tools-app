package fr.huiitre.tools.modules.palworld.catalog.infrastructure;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.catalog.application.ports.ShopCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.view.MerchantOfferView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.MerchantView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.ShopCurrencyView;

public class PostgresShopCatalogRepository implements ShopCatalogRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresShopCatalogRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public List<MerchantView> findAllMerchants() {
        Map<Long, List<MerchantOfferView>> offersByMerchantId = new LinkedHashMap<>();
        final String offersSql = """
                SELECT mo.merchant_id, i.id AS item_id, i.slug AS item_slug, i.name AS item_name, i.icon_url AS item_icon_url,
                       i.max_stack_count AS item_max_stack_count, mo.price, mo.quantity_per_purchase, mo.product_type
                FROM tools_palworld.merchant_offer mo
                JOIN tools_palworld.item i ON i.id = mo.item_id
                ORDER BY mo.merchant_id, i.name
                """;
        jdbcTemplate.query(offersSql, rs -> {
            offersByMerchantId.computeIfAbsent(rs.getLong("merchant_id"), id -> new ArrayList<>())
                    .add(new MerchantOfferView(
                            rs.getLong("item_id"),
                            rs.getString("item_slug"),
                            rs.getString("item_name"),
                            rs.getString("item_icon_url"),
                            (Integer) rs.getObject("item_max_stack_count"),
                            rs.getInt("price"),
                            rs.getInt("quantity_per_purchase"),
                            rs.getString("product_type")));
        });

        final String merchantsSql = """
                SELECT m.id, m.external_id, m.code, m.name, m.portrait_url, m.restock_minute,
                       m.currency_item_id, i.name AS currency_name, i.icon_url AS currency_icon_url
                FROM tools_palworld.merchant m
                LEFT JOIN tools_palworld.item i ON i.slug = m.currency_item_id
                ORDER BY m.name NULLS LAST, m.code
                """;
        return jdbcTemplate.query(merchantsSql, (rs, rowNum) -> {
            Long id = rs.getLong("id");
            ShopCurrencyView currency = new ShopCurrencyView(
                    rs.getString("currency_item_id"), rs.getString("currency_name"), rs.getString("currency_icon_url"));
            return new MerchantView(
                    id,
                    rs.getString("external_id"),
                    rs.getString("code"),
                    rs.getString("name"),
                    rs.getString("portrait_url"),
                    (Integer) rs.getObject("restock_minute"),
                    currency,
                    offersByMerchantId.getOrDefault(id, List.of()));
        });
    }
}
