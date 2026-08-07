package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.sync.application.ports.ItemSyncRepository;

public class PostgresItemSyncRepository implements ItemSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresItemSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public Long upsertItem(String slug, String name, String iconUrl, Integer price, Integer maxStackCount, String category) {
        final String sql = """
                INSERT INTO tools_palworld.item (slug, name, icon_url, price, max_stack_count, category)
                VALUES (?, ?, ?, ?, ?, ?)
                ON CONFLICT (slug) DO UPDATE SET
                    name = EXCLUDED.name, icon_url = EXCLUDED.icon_url,
                    price = EXCLUDED.price, max_stack_count = EXCLUDED.max_stack_count, category = EXCLUDED.category
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class, slug, name, iconUrl, price, maxStackCount, category);
    }
}
