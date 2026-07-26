package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.sql.Timestamp;
import java.time.OffsetDateTime;
import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.palworld.sync.application.SkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.SkillSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.SkillRefView;

public class PostgresSkillSyncRepository implements SkillSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresSkillSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<SkillRefView> ROW_MAPPER = (rs, rowNum) -> new SkillRefView(
            rs.getLong("id"),
            rs.getString("slug"),
            rs.getString("category"),
            rs.getString("name"),
            rs.getString("icon_url"),
            (Long) rs.getObject("element_id"),
            rs.getBigDecimal("cooldown"),
            (Integer) rs.getObject("power"),
            rs.getString("status_effect"),
            rs.getString("description"));

    @Override
    public List<SkillRefView> findAll() {
        final String sql = """
                SELECT id, slug, category, name, icon_url, element_id, cooldown, power, status_effect, description
                FROM tools_palworld.skill
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public Long save(SkillSyncData data, Long elementId) {
        final String sql = """
                INSERT INTO tools_palworld.skill (slug, category, name, icon_url, element_id, cooldown, power, status_effect, description)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class,
                data.getSlug(), data.getCategory(), data.getName(), data.getIconUrl(), elementId,
                data.getCooldown(), data.getPower(), data.getStatusEffect(), data.getDescription());
    }

    @Override
    public void update(Long id, SkillSyncData data, Long elementId) {
        final String sql = """
                UPDATE tools_palworld.skill
                SET category = ?, name = ?, icon_url = ?, element_id = ?, cooldown = ?, power = ?,
                    status_effect = ?, description = ?, updated_at = now()
                WHERE id = ?
                """;
        jdbcTemplate.update(sql,
                data.getCategory(), data.getName(), data.getIconUrl(), elementId,
                data.getCooldown(), data.getPower(), data.getStatusEffect(), data.getDescription(), id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_palworld.skill WHERE id = ?", id);
    }

    @Override
    public void upsertSource(Long skillId, String slug, String sourceUrl, String rawPayloadJson, OffsetDateTime fetchedAt) {
        final String sql = """
                INSERT INTO tools_palworld.skill_source (skill_id, source_code, external_slug, external_url, raw_payload, fetched_at)
                VALUES (?, 'paldb_cc', ?, ?, ?::jsonb, ?)
                ON CONFLICT (skill_id, source_code)
                DO UPDATE SET external_slug = EXCLUDED.external_slug, external_url = EXCLUDED.external_url,
                    raw_payload = EXCLUDED.raw_payload, fetched_at = EXCLUDED.fetched_at
                """;
        jdbcTemplate.update(sql, skillId, slug, sourceUrl, rawPayloadJson, Timestamp.from(fetchedAt.toInstant()));
    }
}
