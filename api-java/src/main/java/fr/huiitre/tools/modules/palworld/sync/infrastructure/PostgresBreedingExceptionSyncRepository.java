package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.sync.application.ports.BreedingExceptionSyncRepository;

public class PostgresBreedingExceptionSyncRepository implements BreedingExceptionSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresBreedingExceptionSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void deleteAll() {
        jdbcTemplate.update("DELETE FROM tools_palworld.breeding_exception");
    }

    @Override
    public boolean insert(Long parentAPalId, String parentAGenderCode, Long parentBPalId, String parentBGenderCode, Long childPalId) {
        final String sql = """
                INSERT INTO tools_palworld.breeding_exception
                    (parent_a_pal_id, parent_a_gender, parent_b_pal_id, parent_b_gender, child_pal_id)
                VALUES (?, ?, ?, ?, ?)
                ON CONFLICT (parent_a_pal_id, parent_a_gender, parent_b_pal_id, parent_b_gender) DO NOTHING
                """;
        return jdbcTemplate.update(sql, parentAPalId, parentAGenderCode, parentBPalId, parentBGenderCode, childPalId) > 0;
    }
}
