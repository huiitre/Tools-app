package fr.huiitre.tools.modules.dofus.workshop.infrastructure;

import java.util.List;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopTagRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

public class PostgresWorkshopTagRepository implements WorkshopTagRepository {
    
    private final JdbcTemplate jdbcTemplate;

    public PostgresWorkshopTagRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<WorkshopTag> WORKSHOP_TAG_ROW_MAPPER = (rs, rowNum) -> WorkshopTag.rehydrate(
        rs.getLong("id"),
        rs.getString("name"),
        rs.getString("color")
    );

    @Override
    public List<WorkshopTag> findAllByUserIdAndGameVersionId(Long userId, Long gameVersionId) {
        
        final String sql = """
            SELECT
                id,
                name,
                color
            FROM tools_dofus.workshop_tag
            WHERE user_id = ? AND game_version_id = ?
        """;

        return jdbcTemplate.query(sql, WORKSHOP_TAG_ROW_MAPPER, userId, gameVersionId);
    }

    @Override
    public Optional<WorkshopTag> findByIdAndUserId(Long userId, Long tagId) {
        final String sql = """
            SELECT
                id,
                name,
                color
            FROM tools_dofus.workshop_tag
            WHERE id = ? AND user_id = ?
        """;

        List<WorkshopTag> tags = jdbcTemplate.query(
            sql,
            WORKSHOP_TAG_ROW_MAPPER,
            tagId,
            userId
        );

        return tags.stream().findFirst();
    }

    @Override
    public boolean existsByUserIdAndName(Long userId, String name) {
        final String sql = """
            SELECT EXISTS (
                SELECT 1
                FROM tools_dofus.workshop_tag
                WHERE user_id = ? AND name = ?
            )
        """;

        return Boolean.TRUE.equals(
            jdbcTemplate.queryForObject(
                sql,
                Boolean.class,
                userId,
                name));
    }

    @Override
    public boolean existsByIdAndUserId(Long userId, Long tagId) {
        final String sql = """
            SELECT EXISTS (
                SELECT 1
                FROM tools_dofus.workshop_tag
                WHERE id = ? AND user_id = ?
            )
        """;

        return Boolean.TRUE.equals(
            jdbcTemplate.queryForObject(
                sql,
                Boolean.class,
                tagId,
                userId));
    }

    @Override
    public Long create(Long gameVersionId, Long userId, WorkshopTag tag) {
        final String sql = """
            INSERT INTO tools_dofus.workshop_tag (game_version_id, user_id, name, color)
            VALUES (?, ?, ?, ?)
            RETURNING id
        """;

        return jdbcTemplate.queryForObject(
            sql,
            Long.class,
            gameVersionId,
            userId,
            tag.getName(),
            tag.getColor()
        );
    }

    @Override
    public void update(Long userId, WorkshopTag tag) {
        final String sql = """
            UPDATE tools_dofus.workshop_tag
            SET name = ?, color = ?
            WHERE id = ? AND user_id = ?
        """;

        jdbcTemplate.update(
            sql,
            tag.getName(),
            tag.getColor(),
            tag.getId(),
            userId
        );
    }

    @Override
    public void delete(Long userId, Long tagId) {
        final String sql = """
            DELETE FROM tools_dofus.workshop_tag
            WHERE id = ? AND user_id = ?
        """;

        jdbcTemplate.update(sql, tagId, userId);
    }
}