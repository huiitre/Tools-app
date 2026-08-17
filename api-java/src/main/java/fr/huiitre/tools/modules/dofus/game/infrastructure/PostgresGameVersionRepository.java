package fr.huiitre.tools.modules.dofus.game.infrastructure;

import java.util.List;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;

public class PostgresGameVersionRepository implements GameVersionRepository {

        private final JdbcTemplate jdbcTemplate;

        public PostgresGameVersionRepository(JdbcTemplate jdbcTemplate) {
                this.jdbcTemplate = jdbcTemplate;
        }

        @Override
        public Optional<GameVersionData> findByGameServerId(Long gameServerId) {
            final String sql = """
                SELECT gv.id, gv.code, gv.name
                FROM tools_dofus.game_version gv
                JOIN tools_dofus.game_server gs ON gs.game_version_id = gv.id
                WHERE gs.id = ?
            """;
            return jdbcTemplate
                    .query(
                        sql,
                        (rs, rowNum) -> new GameVersionData(
                                rs.getLong("id"),
                                rs.getString("name"),
                                rs.getString("code")),
                        gameServerId)
                    .stream()
                    .findFirst();
        }

        @Override
        public Optional<GameVersionData> findById(Long gameVersionId) {
                final String sql = """
                                    SELECT id, code, name
                                    FROM tools_dofus.game_version
                                    WHERE id = ?
                                """;

                return jdbcTemplate
                                .query(
                                                sql,
                                                (rs, rowNum) -> new GameVersionData(
                                                                rs.getLong("id"),
                                                                rs.getString("name"),
                                                                rs.getString("code")),
                                                gameVersionId)
                                .stream()
                                .findFirst();
        }

        @Override
        public List<GameVersionData> findAll() {
                final String sql = """
                                    SELECT id, code, name
                                    FROM tools_dofus.game_version
                                    ORDER BY id ASC
                                """;

                return jdbcTemplate
                                .query(
                                                sql,
                                                (rs, rowNum) -> new GameVersionData(
                                                                rs.getLong("id"),
                                                                rs.getString("name"),
                                                                rs.getString("code")));
        }

        @Override
        public GameVersionData findByCode(String code) {
                final String sql = """
                                    SELECT id, code, name
                                    FROM tools_dofus.game_version
                                    WHERE code = ?
                                """;

                return jdbcTemplate.queryForObject(
                                sql,
                                (rs, rowNum) -> new GameVersionData(
                                                rs.getLong("id"),
                                                rs.getString("name"),
                                                rs.getString("code")),
                                code);
        }
}