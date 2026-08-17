package fr.huiitre.tools.modules.core.security.infrastructure;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;

import javax.sql.DataSource;

import fr.huiitre.tools.modules.core.security.application.ports.UserRoleProvider;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

public class PostgresUserRoleProvider implements UserRoleProvider {

    private final DataSource dataSource;

    public PostgresUserRoleProvider(DataSource dataSource) {
        this.dataSource = dataSource;
    }

    @Override
    public RoleCode getUserRole(String userId, ModuleCode moduleCode) {

        // user_id est BIGINT en base
        long uid = Long.parseLong(userId);

        final String sqlGlobal = """
                    SELECT r.code
                    FROM tools_core.user_role ur
                    JOIN tools_core.role r ON r.id = ur.role_id
                    WHERE ur.user_id = ?
                    LIMIT 1
                """;

        final String sqlModule = """
                    SELECT r.code
                    FROM tools_core.user_module_role umr
                    JOIN tools_core.role r ON r.id = umr.role_id
                    JOIN tools_core.module m ON m.id = umr.module_id
                    WHERE umr.user_id = ?
                      AND m.code = ?
                    LIMIT 1
                """;

        try (
                Connection conn = dataSource.getConnection();
                PreparedStatement ps = conn.prepareStatement(
                        moduleCode == null ? sqlGlobal : sqlModule)) {

            ps.setLong(1, uid);
            if (moduleCode != null) {
                ps.setString(2, moduleCode.name().toLowerCase());
            }

            try (ResultSet rs = ps.executeQuery()) {
                if (!rs.next()) {
                    throw new IllegalStateException(
                            "No role found for user " + userId +
                                    (moduleCode == null ? "" : " in module " + moduleCode.name()));
                }
                return RoleCode.valueOf(rs.getString(1));
            }

        } catch (SQLException e) {
            throw new IllegalStateException("Failed to load user role", e);
        }
    }
}
