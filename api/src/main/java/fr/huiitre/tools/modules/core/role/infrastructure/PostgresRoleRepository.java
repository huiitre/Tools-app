package fr.huiitre.tools.modules.core.role.infrastructure;

import java.util.List;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.role.api.view.RoleView;
import fr.huiitre.tools.modules.core.role.domain.Role;

public class PostgresRoleRepository implements RoleRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresRoleRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<Role> ROLE_ROW_MAPPER = (rs, rowNum) -> new Role(
            rs.getLong("id"),
            rs.getString("code"),
            rs.getString("name"),
            rs.getString("description"),
            rs.getBoolean("is_active"),
            rs.getTimestamp("created_at").toLocalDateTime(),
            rs.getTimestamp("updated_at").toLocalDateTime());

    private static final RowMapper<RoleView> ROLE_VIEW_ROW_MAPPER = (rs, rowNum) -> new RoleView(
            rs.getLong("id"),
            rs.getString("code"),
            rs.getString("name"),
            rs.getString("description"),
            rs.getBoolean("is_active"));

    @Override
    public void save(Role role) {
        final String sql = """
                    INSERT INTO tools_core.role (name, code, description, is_active)
                    VALUES (?, ?, ?, ?)
                    RETURNING id
                """;

        Long id = jdbcTemplate.queryForObject(
                sql,
                Long.class,
                role.getName(),
                role.getCode(),
                role.getDescription(),
                role.getActive());

        role.setId(id);
    }

    @Override
    public Optional<Role> findById(Long id) {
        final String sql = """
                    SELECT id, code, name, description, is_active, created_at, updated_at
                    FROM tools_core.role
                    WHERE id = ?
                """;

        List<Role> results = jdbcTemplate.query(sql, ROLE_ROW_MAPPER, id);
        return results.stream().findFirst();
    }

    @Override
    public Optional<Role> findByCode(String code) {
        final String sql = """
                    SELECT id, code, name, description, is_active, created_at, updated_at
                    FROM tools_core.role
                    WHERE code = ?
                """;

        List<Role> results = jdbcTemplate.query(sql, ROLE_ROW_MAPPER, code);
        return results.stream().findFirst();
    }

    @Override
    public List<RoleView> findAll() {
        final String sql = """
                    SELECT id, code, name, description, is_active, created_at, updated_at
                    FROM tools_core.role
                """;

        return jdbcTemplate.query(sql, ROLE_VIEW_ROW_MAPPER);
    }
}
