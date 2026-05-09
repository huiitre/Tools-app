package fr.huiitre.tools.modules.core.role.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.core.role.application.ports.UserRoleRepository;
import fr.huiitre.tools.modules.core.role.api.view.RoleView;
import fr.huiitre.tools.modules.core.role.domain.UserRole;

public class PostgresUserRoleRepository implements UserRoleRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresUserRoleRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<RoleView> ROLE_VIEW_ROW_MAPPER = (rs, rowNum) -> new RoleView(
            rs.getLong("id"),
            rs.getString("code"),
            rs.getString("name"),
            rs.getString("description"),
            rs.getBoolean("is_active"));

    @Override
    public void save(UserRole userRole) {
        final String sql = """
                    INSERT INTO tools_core.user_role (user_id, role_id)
                    VALUES (?, ?)
                """;

        jdbcTemplate.update(
                sql,
                userRole.getUserId(),
                userRole.getRoleId());
    }

    @Override
    public List<RoleView> findAllByUserId(Long userId) {
        final String sql = """
                    SELECT r.id, r.code, r.name, r.description, r.is_active, r.created_at, r.updated_at
                    FROM tools_core.role r
                    JOIN tools_core.user_role ur ON r.id = ur.role_id
                    WHERE ur.user_id = ?
                """;

        return jdbcTemplate.query(
                sql,
                ROLE_VIEW_ROW_MAPPER,
                userId);
    }

    @Override
    public void deleteAllByUserId(Long userId) {
        final String sql = """
                    DELETE FROM tools_core.user_role
                    WHERE user_id = ?
                """;

        jdbcTemplate.update(sql, userId);
    }
}
