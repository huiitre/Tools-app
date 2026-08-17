package fr.huiitre.tools.modules.palworld.serverdata.infrastructure;

import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.serverdata.application.ports.PalLookupRepository;

public class PostgresPalLookupRepository implements PalLookupRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresPalLookupRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public Map<String, Long> findIdByTribeUpper() {
        List<Object[]> rows = jdbcTemplate.query(
                "SELECT id, tribe FROM tools_palworld.pal",
                (rs, rowNum) -> new Object[] { rs.getLong("id"), rs.getString("tribe") });

        return rows.stream().collect(Collectors.toMap(
                row -> ((String) row[1]).toUpperCase(),
                row -> (Long) row[0],
                (existing, duplicate) -> existing));
    }
}
