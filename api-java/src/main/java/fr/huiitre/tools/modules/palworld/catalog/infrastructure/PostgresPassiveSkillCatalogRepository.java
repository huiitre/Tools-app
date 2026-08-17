package fr.huiitre.tools.modules.palworld.catalog.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.catalog.application.ports.PassiveSkillCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.view.PassiveSkillCatalogItemView;

public class PostgresPassiveSkillCatalogRepository implements PassiveSkillCatalogRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresPassiveSkillCatalogRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public List<PassiveSkillCatalogItemView> findAll() {
        final String sql = """
                SELECT id, name, description, rank, rank_icon_url, is_negative, is_world_tree
                FROM tools_palworld.passive_skill
                ORDER BY is_negative, rank DESC, name
                """;
        return jdbcTemplate.query(sql, (rs, rowNum) -> new PassiveSkillCatalogItemView(
                rs.getString("id"), rs.getString("name"), rs.getString("description"), rs.getInt("rank"),
                rs.getString("rank_icon_url"), rs.getBoolean("is_negative"), rs.getBoolean("is_world_tree")));
    }
}
