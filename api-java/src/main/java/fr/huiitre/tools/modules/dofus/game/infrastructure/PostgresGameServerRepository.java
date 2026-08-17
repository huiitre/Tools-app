package fr.huiitre.tools.modules.dofus.game.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.dofus.game.application.ports.GameServerRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameServerData;

public class PostgresGameServerRepository implements GameServerRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresGameServerRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<GameServerData> GAME_SERVER_DATA_ROW_MAPPER = (rs, rowNum) -> new GameServerData(
            rs.getLong("id"),
            rs.getLong("game_version_id"),
            rs.getString("name"),
            rs.getString("code"));

    @Override
    public List<GameServerData> findAllByGameVersionId(Long gameVersionId) {
        final String sql = """
                    SELECT
                        id,
                        game_version_id,
                        name,
                        code
                    FROM tools_dofus.game_server
                    WHERE game_version_id = ?
                """;

        return jdbcTemplate.query(sql, GAME_SERVER_DATA_ROW_MAPPER, gameVersionId);
    }
}
