package fr.huiitre.tools.modules.core.user_module.infrastructure;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.core.user_module.application.ports.UserModuleRoleRepository;
import fr.huiitre.tools.modules.core.user_module.application.view.ModuleUserView;
import fr.huiitre.tools.modules.core.user_module.application.view.UserModuleView;
import fr.huiitre.tools.modules.core.role.api.view.RoleView;
import fr.huiitre.tools.modules.core.user_module.domain.UserModuleRole;

public class PostgresUserModuleRoleRepository implements UserModuleRoleRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresUserModuleRoleRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void save(UserModuleRole userModuleRole) {
        final String sql = """
                    INSERT INTO tools_core.user_module_role (user_id, module_id, role_id)
                    VALUES (?, ?, ?)
                """;

        jdbcTemplate.update(
                sql,
                userModuleRole.getUserId(),
                userModuleRole.getModuleId(),
                userModuleRole.getRoleId());
    }

    @Override
    public void deleteByUserIdAndModuleId(UserModuleRole userModuleRole) {
        final String sql = """
                    DELETE FROM tools_core.user_module_role
                    WHERE user_id = ? AND module_id = ?
                """;

        jdbcTemplate.update(
                sql,
                userModuleRole.getUserId(),
                userModuleRole.getModuleId());
    }

    @Override
    public Optional<UserModuleRole> findByUserIdAndModuleId(Long userId, Long moduleId) {
        final String sql = """
                    SELECT user_id, module_id, role_id
                    FROM tools_core.user_module_role
                    WHERE user_id = ? AND module_id = ?
                """;

        return jdbcTemplate
                .query(
                        sql,
                        (rs, rowNum) -> new UserModuleRole(
                                rs.getLong("user_id"),
                                rs.getLong("module_id"),
                                rs.getLong("role_id")),
                        userId,
                        moduleId)
                .stream()
                .findFirst();
    }

    @Override
    public void deleteByModuleId(Long moduleId) {
        final String sql = """
                    DELETE FROM tools_core.user_module_role
                    WHERE module_id = ?
                """;

        jdbcTemplate.update(sql, moduleId);
    }

    @Override
    public void updateRoleId(UserModuleRole userModuleRole) {
        final String sql = """
                    UPDATE tools_core.user_module_role
                    SET role_id = ?
                    WHERE user_id = ? AND module_id = ?
                """;

        jdbcTemplate.update(
                sql,
                userModuleRole.getRoleId(),
                userModuleRole.getUserId(),
                userModuleRole.getModuleId());
    }

    @Override
    public List<UserModuleView> findAllByUserId(Long userId) {
        final String sql = """
                    SELECT
                        m.id,
                        m.code,
                        m.name,
                        m.description,
                        m.is_active,
                        r.id AS role_id,
                        r.code AS role_code,
                        r.name AS role_name,
                        r.description AS role_description,
                        r.is_active AS role_is_active
                    FROM tools_core.user_module_role umr
                    INNER JOIN tools_core.module m ON m.id = umr.module_id
                    INNER JOIN tools_core.role r ON r.id = umr.role_id
                    WHERE umr.user_id = ?
                    ORDER BY m.id, r.id
                """;

        Map<Long, UserModuleView> moduleById = new LinkedHashMap<>();

        jdbcTemplate.query(
                sql,
                rs -> {
                    long moduleId = rs.getLong("id");

                    UserModuleView module = moduleById.get(moduleId);
                    if (module == null) {
                        module = new UserModuleView(
                                moduleId,
                                rs.getString("code"),
                                rs.getString("name"),
                                rs.getString("description"),
                                rs.getBoolean("is_active"),
                                new ArrayList<>());
                        moduleById.put(moduleId, module);
                    }

                    RoleView role = new RoleView(
                            rs.getLong("role_id"),
                            rs.getString("role_code"),
                            rs.getString("role_name"),
                            rs.getString("role_description"),
                            rs.getBoolean("role_is_active"));

                    module.getRoles().add(role);
                },
                userId);

        return new ArrayList<>(moduleById.values());
    }

    @Override
    public List<ModuleUserView> findAllByModuleId(Long moduleId) {
        final String sql = """
                    SELECT u.id AS user_id, u.email, u.name, r.id AS role_id, r.code AS role_code
                    FROM tools_core.user_module_role umr
                    INNER JOIN tools_core.users u ON u.id = umr.user_id
                    INNER JOIN tools_core.role r ON r.id = umr.role_id
                    WHERE umr.module_id = ?
                    ORDER BY u.name
                """;

        return jdbcTemplate.query(
                sql,
                (rs, rowNum) -> new ModuleUserView(
                        rs.getLong("user_id"),
                        rs.getString("email"),
                        rs.getString("name"),
                        rs.getLong("role_id"),
                        rs.getString("role_code")),
                moduleId);
    }
}
