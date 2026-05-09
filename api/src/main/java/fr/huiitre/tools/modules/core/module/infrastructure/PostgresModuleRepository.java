package fr.huiitre.tools.modules.core.module.infrastructure;

import java.util.List;
import java.util.Optional;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.core.module.application.ports.ModuleRepository;
import fr.huiitre.tools.modules.core.module.domain.Module;

public class PostgresModuleRepository implements ModuleRepository {

    private static final Logger logger = LoggerFactory.getLogger(PostgresModuleRepository.class);

    private final JdbcTemplate jdbcTemplate;

    public PostgresModuleRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<Module> MODULE_ROW_MAPPER = (rs, rowNum) -> {
        Module module = new Module(
                rs.getString("code"),
                rs.getString("name"),
                rs.getString("description"));
        module.setId(rs.getLong("id"));
        module.setActive(rs.getBoolean("is_active"));
        module.setCreatedAt(rs.getTimestamp("created_at").toLocalDateTime());
        module.setUpdatedAt(
                rs.getTimestamp("updated_at") != null
                        ? rs.getTimestamp("updated_at").toLocalDateTime()
                        : null);
        return module;
    };

    @Override
    public void save(Module module) {
        final String sql = """
                    INSERT INTO tools_core.module (name, description, code, is_active)
                    VALUES (?, ?, ?, ?)
                    RETURNING id
                """;

        Long id = jdbcTemplate.queryForObject(
                sql,
                Long.class,
                module.getName(),
                module.getDescription(),
                module.getCode(),
                module.getActive());

        module.setId(id);
    }

    @Override
    public void delete(Module module) {
        final String sql = """
                    DELETE FROM tools_core.module
                    WHERE id = ?
                """;

        jdbcTemplate.update(sql, module.getId());
    }

    @Override
    public void update(Module module) {
        final String sql = """
                    UPDATE tools_core.module
                    SET name = ?, description = ?, code = ?, is_active = ?
                    WHERE id = ?
                """;

        jdbcTemplate.update(
                sql,
                module.getName(),
                module.getDescription(),
                module.getCode(),
                module.getActive(),
                module.getId());
    }

    @Override
    public Optional<Module> findById(Long id) {
        final String sql = """
                    SELECT id, name, description, code, is_active, created_at, updated_at
                    FROM tools_core.module
                    WHERE id = ?
                """;

        List<Module> result = jdbcTemplate.query(sql, MODULE_ROW_MAPPER, id);
        return result.stream().findFirst();
    }

    @Override
    public boolean existsByCode(String code) {
        final String sql = """
                    SELECT EXISTS(
                        SELECT 1 FROM tools_core.module WHERE code = ?
                    )
                """;

        return Boolean.TRUE.equals(
                jdbcTemplate.queryForObject(sql, Boolean.class, code));
    }

    public List<Module> findAll() {
        final String sql = """
                    SELECT id, name, description, code, is_active, created_at, updated_at
                    FROM tools_core.module
                """;

        return jdbcTemplate.query(sql, MODULE_ROW_MAPPER);
    }
}
