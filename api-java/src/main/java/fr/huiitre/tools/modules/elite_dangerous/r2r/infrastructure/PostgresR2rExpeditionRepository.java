package fr.huiitre.tools.modules.elite_dangerous.r2r.infrastructure;

import java.sql.Array;
import java.sql.Timestamp;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import java.util.stream.Collectors;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.elite_dangerous.r2r.application.ports.R2rExpeditionRepository;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.view.R2rExpeditionDetailView;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.view.R2rExpeditionSummaryView;
import fr.huiitre.tools.modules.elite_dangerous.r2r.domain.R2rExpedition;

public class PostgresR2rExpeditionRepository implements R2rExpeditionRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresR2rExpeditionRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<R2rExpeditionSummaryView> SUMMARY_ROW_MAPPER = (rs, rowNum) ->
            new R2rExpeditionSummaryView(
                    UUID.fromString(rs.getString("id")),
                    rs.getString("name"),
                    rs.getString("source"),
                    rs.getInt("current_system_index"),
                    rs.getInt("total_systems"),
                    rs.getTimestamp("created_at").toLocalDateTime(),
                    rs.getTimestamp("updated_at").toLocalDateTime());

    private static final RowMapper<R2rExpeditionDetailView> DETAIL_ROW_MAPPER = (rs, rowNum) -> {
        Array pgArray = rs.getArray("current_bodies_done");
        List<Long> bodies = pgArray == null ? Collections.emptyList()
                : Arrays.stream((Long[]) pgArray.getArray()).collect(Collectors.toList());
        return new R2rExpeditionDetailView(
                UUID.fromString(rs.getString("id")),
                rs.getString("name"),
                rs.getString("source"),
                rs.getString("route_data"),
                rs.getInt("current_system_index"),
                bodies,
                rs.getTimestamp("created_at").toLocalDateTime(),
                rs.getTimestamp("updated_at").toLocalDateTime());
    };

    @Override
    public List<R2rExpeditionSummaryView> findAllByUserId(Long userId) {
        final String sql = """
                SELECT id, name, source, current_system_index,
                       jsonb_array_length(route_data->'result') AS total_systems,
                       created_at, updated_at
                FROM tools_elite_dangerous.r2r_expedition
                WHERE user_id = ?
                ORDER BY updated_at DESC
                """;
        return jdbcTemplate.query(sql, SUMMARY_ROW_MAPPER, userId);
    }

    @Override
    public Optional<R2rExpeditionDetailView> findByIdAndUserId(UUID id, Long userId) {
        final String sql = """
                SELECT id, name, source, route_data, current_system_index,
                       current_bodies_done, created_at, updated_at
                FROM tools_elite_dangerous.r2r_expedition
                WHERE id = ? AND user_id = ?
                """;
        return jdbcTemplate.query(sql, DETAIL_ROW_MAPPER, id, userId).stream().findFirst();
    }

    @Override
    public Optional<String> findRouteDataByIdAndUserId(UUID id, Long userId) {
        final String sql = """
                SELECT route_data::text
                FROM tools_elite_dangerous.r2r_expedition
                WHERE id = ? AND user_id = ?
                """;
        return jdbcTemplate.query(sql, (rs, rowNum) -> rs.getString(1), id, userId).stream().findFirst();
    }

    @Override
    public UUID save(Long userId, R2rExpedition expedition) {
        final String sql = """
                INSERT INTO tools_elite_dangerous.r2r_expedition (user_id, name, source, route_data)
                VALUES (?, ?, ?, ?::jsonb)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, UUID.class,
                userId, expedition.getName(), expedition.getSource(), expedition.getRouteData());
    }

    @Override
    public void updateProgress(UUID id, Long userId, int currentSystemIndex, List<Long> currentBodiesDone) {
        String arrayLiteral = "{" + currentBodiesDone.stream()
                .map(String::valueOf)
                .collect(Collectors.joining(",")) + "}";
        final String sql = """
                UPDATE tools_elite_dangerous.r2r_expedition
                SET current_system_index = ?,
                    current_bodies_done  = ?::bigint[],
                    updated_at           = now()
                WHERE id = ? AND user_id = ?
                """;
        jdbcTemplate.update(sql, currentSystemIndex, arrayLiteral, id, userId);
    }

    @Override
    public void rename(UUID id, Long userId, String name) {
        final String sql = """
                UPDATE tools_elite_dangerous.r2r_expedition
                SET name = ?, updated_at = now()
                WHERE id = ? AND user_id = ?
                """;
        jdbcTemplate.update(sql, name, id, userId);
    }

    @Override
    public void delete(UUID id, Long userId) {
        final String sql = """
                DELETE FROM tools_elite_dangerous.r2r_expedition
                WHERE id = ? AND user_id = ?
                """;
        jdbcTemplate.update(sql, id, userId);
    }

    @Override
    public boolean existsByIdAndUserId(UUID id, Long userId) {
        final String sql = """
                SELECT 1 FROM tools_elite_dangerous.r2r_expedition
                WHERE id = ? AND user_id = ?
                """;
        return jdbcTemplate.query(sql, (rs, rowNum) -> true, id, userId).stream().findFirst().orElse(false);
    }

    @Override
    public int countByUserId(Long userId) {
        final String sql = """
                SELECT COUNT(*) FROM tools_elite_dangerous.r2r_expedition
                WHERE user_id = ?
                """;
        Integer count = jdbcTemplate.queryForObject(sql, Integer.class, userId);
        return count != null ? count : 0;
    }
}
