package fr.huiitre.tools.modules.core.admin.infrastructure;

import fr.huiitre.tools.modules.core.admin.application.ports.AdminStatsRepository;
import fr.huiitre.tools.modules.core.admin.application.view.AdminStatsView;
import org.springframework.jdbc.core.JdbcTemplate;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Map;

public class PostgresAdminStatsRepository implements AdminStatsRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresAdminStatsRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public AdminStatsView getStats() {
        final String userCountSql = """
                    SELECT
                        COUNT(*) AS total,
                        COUNT(*) FILTER (WHERE is_active = true) AS active
                    FROM tools_core.users
                    WHERE user_type = 'HUMAN'
                """;

        Map<String, Object> counts = jdbcTemplate.queryForMap(userCountSql);
        long totalUsers = ((Number) counts.get("total")).longValue();
        long activeUsers = ((Number) counts.get("active")).longValue();

        final String newUsersSql = """
                    SELECT COUNT(*) FROM tools_core.users
                    WHERE user_type = 'HUMAN'
                    AND created_at >= ?
                """;

        Long newCount = jdbcTemplate.queryForObject(newUsersSql, Long.class, LocalDateTime.now().minusWeeks(1));
        long newUsersThisWeek = newCount != null ? newCount : 0;

        final String perModuleSql = """
                    SELECT m.code, m.name, COUNT(DISTINCT umr.user_id) AS user_count
                    FROM tools_core.module m
                    LEFT JOIN tools_core.user_module_role umr ON m.id = umr.module_id
                    GROUP BY m.id, m.code, m.name
                    ORDER BY user_count DESC
                """;

        List<AdminStatsView.ModuleUserCount> usersPerModule = jdbcTemplate.query(
                perModuleSql,
                (rs, rowNum) -> new AdminStatsView.ModuleUserCount(
                        rs.getString("code"),
                        rs.getString("name"),
                        rs.getLong("user_count")));

        return new AdminStatsView(totalUsers, activeUsers, newUsersThisWeek, usersPerModule);
    }
}
