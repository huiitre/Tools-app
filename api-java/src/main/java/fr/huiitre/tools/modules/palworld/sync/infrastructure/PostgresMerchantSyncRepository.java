package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.Set;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.sync.application.ports.MerchantSyncRepository;

public class PostgresMerchantSyncRepository implements MerchantSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresMerchantSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public Long upsertMerchant(
            String externalId, String code, String name, String portraitUrl, Integer restockMinute, String currencyItemId) {
        final String sql = """
                INSERT INTO tools_palworld.merchant (external_id, code, name, portrait_url, restock_minute, currency_item_id)
                VALUES (?, ?, ?, ?, ?, ?)
                ON CONFLICT (external_id) DO UPDATE SET
                    code = EXCLUDED.code, name = EXCLUDED.name, portrait_url = EXCLUDED.portrait_url,
                    restock_minute = EXCLUDED.restock_minute, currency_item_id = EXCLUDED.currency_item_id
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class, externalId, code, name, portraitUrl, restockMinute, currencyItemId);
    }

    @Override
    public int deleteAllNotIn(Set<String> externalIds) {
        if (externalIds.isEmpty()) {
            return jdbcTemplate.update("DELETE FROM tools_palworld.merchant");
        }
        return jdbcTemplate.update(con -> {
            var ps = con.prepareStatement("DELETE FROM tools_palworld.merchant WHERE NOT (external_id = ANY (?))");
            ps.setArray(1, con.createArrayOf("varchar", externalIds.toArray()));
            return ps;
        });
    }

    @Override
    public void deleteOffers(Long merchantId) {
        jdbcTemplate.update("DELETE FROM tools_palworld.merchant_offer WHERE merchant_id = ?", merchantId);
    }

    @Override
    public boolean insertOffer(Long merchantId, Long itemId, int price, int quantityPerPurchase, String productType) {
        final String sql = """
                INSERT INTO tools_palworld.merchant_offer (merchant_id, item_id, price, quantity_per_purchase, product_type)
                VALUES (?, ?, ?, ?, ?)
                ON CONFLICT (merchant_id, item_id) DO NOTHING
                """;
        return jdbcTemplate.update(sql, merchantId, itemId, price, quantityPerPurchase, productType) > 0;
    }
}
