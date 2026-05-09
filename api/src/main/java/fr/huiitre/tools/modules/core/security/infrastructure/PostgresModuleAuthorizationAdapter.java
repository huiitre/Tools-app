package fr.huiitre.tools.modules.core.security.infrastructure;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;

import javax.sql.DataSource;

import fr.huiitre.tools.modules.core.security.application.ports.ModuleAuthorizationPort;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;

public class PostgresModuleAuthorizationAdapter implements ModuleAuthorizationPort {

    private final DataSource dataSource;

    public PostgresModuleAuthorizationAdapter(DataSource dataSource) {
        this.dataSource = dataSource;
    }

    @Override
    public boolean hasAccess(String userId, ModuleCode moduleCode) {

        final String sql = """
                    SELECT 1
                    FROM tools_core.user_module_role umr
                    JOIN tools_core.module m ON m.id = umr.module_id
                    WHERE umr.user_id = ?
                      AND m.code = ?
                      AND m.is_active = true
                    LIMIT 1
                """;

        try (
                Connection conn = dataSource.getConnection();
                PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setLong(1, Long.parseLong(userId));
            ps.setString(2, moduleCode.name().toLowerCase());

            try (ResultSet rs = ps.executeQuery()) {
                return rs.next();
            }

        } catch (SQLException e) {
            throw new IllegalStateException(
                    "Failed to check module authorization for user " + userId
                            + " and module " + moduleCode.name(),
                    e);
        }
    }
}
