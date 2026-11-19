package fr.huiitre.tools.modules.dofus.job.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.support.JdbcUtils;

import fr.huiitre.tools.modules.dofus.job.application.ports.JobRepository;
import fr.huiitre.tools.modules.dofus.job.domain.Job;

public class PostgresJobRepository implements JobRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresJobRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<Job> JOB_ROW_MAPPER = (rs, rowNum) -> Job.rehydrate(
            rs.getLong("id"),
            rs.getLong("asset_id"),
            rs.getLong("game_version_id"),
            rs.getString("name"),
            (Long) JdbcUtils.getResultSetValue(rs, rs.findColumn("icon_id"), Long.class)
    );

    @Override
    public List<Job> findAllByGameVersionId(Long gameVersionId) {
        final String sql = """
            SELECT id, asset_id, game_version_id, name, icon_id
            FROM tools_dofus.job
            WHERE game_version_id = ?
        """;

        return jdbcTemplate.query(sql, JOB_ROW_MAPPER, gameVersionId);
    }

    @Override
    public Long insert(Job job) {
        final String sql = """
            INSERT INTO tools_dofus.job (asset_id, game_version_id, name, icon_id)
            VALUES (?, ?, ?, ?)
            RETURNING id
        """;

        return jdbcTemplate.queryForObject(
            sql,
            Long.class,
            job.getAssetId(),
            job.getGameVersionId(),
            job.getName(),
            job.getIconId()
        );
    }

    @Override
    public void update(Job job) {
        final String sql = """
            UPDATE tools_dofus.job
            SET name = ?, icon_id = ?
            WHERE id = ?
        """;

        jdbcTemplate.update(sql, job.getName(), job.getIconId(), job.getId());
    }
}
