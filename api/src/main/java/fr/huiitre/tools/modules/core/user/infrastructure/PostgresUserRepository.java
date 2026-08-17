package fr.huiitre.tools.modules.core.user.infrastructure;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.user.domain.AvatarSource;
import fr.huiitre.tools.modules.core.user.domain.User;
import fr.huiitre.tools.modules.core.user.domain.UserType;

public class PostgresUserRepository implements UserRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresUserRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<User> USER_ROW_MAPPER = (rs, rowNum) -> {
        User user = new User(
                rs.getString("name"),
                rs.getString("email"),
                UserType.valueOf(rs.getString("user_type")),
                AvatarSource.valueOf(rs.getString("avatar_source")));
        user.setId(rs.getLong("id"));
        user.setIsActive(rs.getBoolean("is_active"));
        return user;
    };

    @Override
    public void save(User user) {
        if (user.getId() == null) {
            insert(user);
        } else {
            update(user);
        }
    }

    private void insert(User user) {
        final String sql = """
                    INSERT INTO tools_core.users (name, email, is_active, user_type, avatar_source)
                    VALUES (?, ?, ?, ?, ?)
                    RETURNING id
                """;

        Long id = jdbcTemplate.queryForObject(
                sql,
                Long.class,
                user.getName(),
                user.getEmail(),
                user.isActive(),
                user.getUserType().name(),
                user.getAvatarSource().name());

        user.setId(id);
    }

    private void update(User user) {
        final String sql = """
                    UPDATE tools_core.users
                    SET name = ?,
                        email = ?,
                        is_active = ?,
                        user_type = ?,
                        avatar_source = ?
                    WHERE id = ?
                """;

        jdbcTemplate.update(
                sql,
                user.getName(),
                user.getEmail(),
                user.isActive(),
                user.getUserType().name(),
                user.getAvatarSource().name(),
                user.getId());
    }

    @Override
    public Optional<User> findByEmail(String email) {
        final String sql = """
                    SELECT id, name, email, is_active, user_type, avatar_source
                    FROM tools_core.users
                    WHERE email = ?
                    LIMIT 1
                """;

        List<User> results = jdbcTemplate.query(sql, USER_ROW_MAPPER, email);
        return results.stream().findFirst();
    }

    @Override
    public Optional<User> findById(Long id) {
        final String sql = """
                    SELECT id, name, email, is_active, user_type, avatar_source
                    FROM tools_core.users
                    WHERE id = ?
                    LIMIT 1
                """;

        List<User> results = jdbcTemplate.query(sql, USER_ROW_MAPPER, id);
        return results.stream().findFirst();
    }

    @Override
    public void deleteUnvalidatedUsersWithExpiredEmailVerification(LocalDateTime now) {
        final String sql = """
                    DELETE FROM tools_core.users u
                    USING tools_core.user_email_verification v
                    WHERE u.id = v.user_id
                    AND u.is_active = false
                    AND v.expires_at <= ?
                """;

        jdbcTemplate.update(sql, now);
    }

    @Override
    public void deleteUnvalidatedUsersWithoutEmailVerification() {
        final String sql = """
                    DELETE FROM tools_core.users u
                    WHERE u.is_active = false
                    AND NOT EXISTS (
                        SELECT 1
                        FROM tools_core.user_email_verification v
                        WHERE v.user_id = u.id
                    )
                """;

        jdbcTemplate.update(sql);
    }

    @Override
    public List<Long> findAllIds() {
        final String sql = "SELECT id FROM tools_core.users WHERE is_active = TRUE";
        return jdbcTemplate.queryForList(sql, Long.class);
    }

    @Override
    public List<Long> findAllIdsByRoleId(Long roleId) {
        final String sql = """
                    SELECT u.id FROM tools_core.users u
                    JOIN tools_core.user_role ur ON u.id = ur.user_id
                    WHERE ur.role_id = ? AND u.is_active = TRUE
                """;
        return jdbcTemplate.queryForList(sql, Long.class, roleId);
    }

    @Override
    public List<Long> findAllIdsByRoleCodes(List<String> roleCodes) {
        if (roleCodes == null || roleCodes.isEmpty()) return List.of();
        String placeholders = roleCodes.stream().map(c -> "?").collect(java.util.stream.Collectors.joining(", "));
        String sql = """
                    SELECT DISTINCT u.id FROM tools_core.users u
                    JOIN tools_core.user_role ur ON u.id = ur.user_id
                    JOIN tools_core.role r ON ur.role_id = r.id
                    WHERE r.code IN (%s) AND u.is_active = TRUE
                """.formatted(placeholders);
        return jdbcTemplate.queryForList(sql, Long.class, roleCodes.toArray());
    }

    @Override
    public List<Long> findAllIdsByModuleId(Long moduleId) {
        final String sql = """
                    SELECT u.id FROM tools_core.users u
                    JOIN tools_core.user_module_role umr ON u.id = umr.user_id
                    WHERE umr.module_id = ? AND u.is_active = TRUE
                """;
        return jdbcTemplate.queryForList(sql, Long.class, moduleId);
    }
}
