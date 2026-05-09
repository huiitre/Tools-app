package fr.huiitre.tools.modules.dofus.almanax.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxRepository;
import fr.huiitre.tools.modules.dofus.almanax.domain.Almanax;

public class PostgresAlmanaxRepository implements AlmanaxRepository {

    private final JdbcTemplate jdbcTemplate;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PostgresAlmanaxRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private final RowMapper<Almanax> ROW_MAPPER = (rs, rowNum) -> {
        try {
            List<String> dates = objectMapper.readValue(
                    rs.getString("dates"),
                    new TypeReference<List<String>>() {
                    });

            return Almanax.rehydrate(
                    rs.getLong("id"),
                    rs.getLong("asset_id"),
                    rs.getString("name"),
                    rs.getString("description"),
                    dates,
                    rs.getLong("item_id"),
                    rs.getLong("item_quantity"));
        } catch (JsonProcessingException e) {
            throw new RuntimeException("Failed to parse dates JSON", e);
        }
    };

    @Override
    public List<Almanax> findAll() {
        String sql = """
                    SELECT
                        id,
                        asset_id,
                        name,
                        description,
                        dates,
                        item_id,
                        item_quantity
                    FROM
                        tools_dofus.almanax
                """;

        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public Long save(Almanax almanax) {
        String sql = """
                    INSERT INTO tools_dofus.almanax (
                        asset_id,
                        name,
                        description,
                        dates,
                        item_id,
                        item_quantity
                    ) VALUES (?, ?, ?, ?::jsonb, ?, ?)
                    RETURNING id
                """;

        try {
            String jsonDates = objectMapper.writeValueAsString(almanax.getDates());

            return jdbcTemplate.queryForObject(
                    sql,
                    Long.class,
                    almanax.getAssetId(),
                    almanax.getName(),
                    almanax.getDescription(),
                    jsonDates,
                    almanax.getItemId(),
                    almanax.getItemQuantity());
        } catch (JsonProcessingException e) {
            throw new RuntimeException("Failed to serialize dates to JSON", e);
        }
    }

    @Override
    public void update(Almanax almanax) {
        String sql = """
                    UPDATE tools_dofus.almanax
                    SET
                        name = ?,
                        description = ?,
                        dates = ?::jsonb,
                        item_id = ?,
                        item_quantity = ?
                    WHERE
                        id = ?
                """;

        try {
            String jsonDates = objectMapper.writeValueAsString(almanax.getDates());

            jdbcTemplate.update(
                    sql,
                    almanax.getName(),
                    almanax.getDescription(),
                    jsonDates,
                    almanax.getItemId(),
                    almanax.getItemQuantity(),
                    almanax.getId());
        } catch (JsonProcessingException e) {
            throw new RuntimeException("Failed to serialize dates to JSON", e);
        }
    }
}
