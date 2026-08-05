package fr.huiitre.tools.modules.palworld.breeding.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.palworld.breeding.application.ports.BreedingCatalogRepository;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingException;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;
import fr.huiitre.tools.modules.palworld.domain.breeding.Gender;

public class PostgresBreedingCatalogRepository implements BreedingCatalogRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresBreedingCatalogRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<BreedingPal> PAL_ROW_MAPPER = (rs, rowNum) -> new BreedingPal(
            rs.getLong("id"),
            rs.getString("tribe"),
            rs.getString("name"),
            (Integer) rs.getObject("combi_rank"),
            (Integer) rs.getObject("combi_duplicate_priority"),
            rs.getBoolean("ignore_combi"));

    private static final RowMapper<BreedingException> EXCEPTION_ROW_MAPPER = (rs, rowNum) -> new BreedingException(
            rs.getLong("parent_a_pal_id"),
            Gender.fromCode(rs.getString("parent_a_gender")),
            rs.getLong("parent_b_pal_id"),
            Gender.fromCode(rs.getString("parent_b_gender")),
            rs.getLong("child_pal_id"));

    @Override
    public List<BreedingPal> findAllPals() {
        final String sql = """
                SELECT id, tribe, name, combi_rank, combi_duplicate_priority, ignore_combi
                FROM tools_palworld.pal
                """;
        return jdbcTemplate.query(sql, PAL_ROW_MAPPER);
    }

    @Override
    public List<BreedingException> findAllExceptions() {
        final String sql = """
                SELECT parent_a_pal_id, parent_a_gender, parent_b_pal_id, parent_b_gender, child_pal_id
                FROM tools_palworld.breeding_exception
                """;
        return jdbcTemplate.query(sql, EXCEPTION_ROW_MAPPER);
    }
}
