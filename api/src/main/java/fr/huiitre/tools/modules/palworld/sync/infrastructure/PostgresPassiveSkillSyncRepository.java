package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.sync.application.PassiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PassiveSkillSyncRepository;

public class PostgresPassiveSkillSyncRepository implements PassiveSkillSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresPassiveSkillSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public List<String> findAllIds() {
        return jdbcTemplate.queryForList("SELECT id FROM tools_palworld.passive_skill", String.class);
    }

    @Override
    public void upsert(PassiveSkillSyncData passiveSkill) {
        final String sql = """
                INSERT INTO tools_palworld.passive_skill
                    (id, name, description, rank, rank_icon_url, is_negative, is_world_tree, raw_payload)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?::jsonb)
                ON CONFLICT (id) DO UPDATE
                SET name = EXCLUDED.name, description = EXCLUDED.description, rank = EXCLUDED.rank,
                    rank_icon_url = EXCLUDED.rank_icon_url, is_negative = EXCLUDED.is_negative,
                    is_world_tree = EXCLUDED.is_world_tree, raw_payload = EXCLUDED.raw_payload, updated_at = now()
                """;
        jdbcTemplate.update(sql,
                passiveSkill.id(), passiveSkill.name(), passiveSkill.description(), passiveSkill.rank(),
                passiveSkill.rankIconUrl(), passiveSkill.negative(), passiveSkill.worldTree(), passiveSkill.rawPayloadJson());
    }

    @Override
    public void deleteById(String id) {
        jdbcTemplate.update("DELETE FROM tools_palworld.passive_skill WHERE id = ?", id);
    }
}
