package fr.huiitre.tools.modules.core.notification.infrastructure.persistence;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.List;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.core.notification.application.port.NotificationRepository;
import fr.huiitre.tools.modules.core.notification.domain.entity.Notification;
import fr.huiitre.tools.modules.core.notification.domain.entity.NotificationType;

public class PostgresNotificationRepository implements NotificationRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresNotificationRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<Notification> NOTIFICATION_ROW_MAPPER = (rs, rowNum) -> new Notification(
            rs.getLong("id"),
            rs.getString("title"),
            rs.getString("body"),
            NotificationType.valueOf(rs.getString("type")),
            null, null, null, 
            rs.getString("metadata"),
            rs.getTimestamp("created_at").toLocalDateTime(),
            rs.getBoolean("is_read")
    );

    @Override
    public Notification save(Notification notification) {
        final String sql = """
                    INSERT INTO tools_core.notifications (title, body, type, target_user_id, target_role_id, target_module_id, metadata)
                    VALUES (?, ?, ?, ?, ?, ?, ?::jsonb)
                    RETURNING id, created_at
                """;

        return jdbcTemplate.queryForObject(
                sql,
                (rs, rowNum) -> new Notification(
                        rs.getLong("id"),
                        notification.title(),
                        notification.body(),
                        notification.type(),
                        notification.targetUserId(),
                        notification.targetRoleId(),
                        notification.targetModuleId(),
                        notification.metadata(),
                        rs.getTimestamp("created_at").toLocalDateTime(),
                        false),
                notification.title(),
                notification.body(),
                notification.type().name(),
                notification.targetUserId(),
                notification.targetRoleId(),
                notification.targetModuleId(),
                notification.metadata());
    }

    @Override
    public void createUserEntries(Long notificationId, List<Long> userIds) {
        final String sql = "INSERT INTO tools_core.user_notifications (user_id, notification_id) VALUES (?, ?)";
        List<Object[]> batchArgs = userIds.stream()
                .map(userId -> new Object[]{userId, notificationId})
                .toList();
        jdbcTemplate.batchUpdate(sql, batchArgs);
    }

    @Override
    public Optional<Notification> findById(Long id) {
        final String sql = "SELECT n.*, FALSE as is_read FROM tools_core.notifications n WHERE n.id = ?";
        List<Notification> results = jdbcTemplate.query(sql, NOTIFICATION_ROW_MAPPER, id);
        return results.stream().findFirst();
    }

    @Override
    public List<Notification> findActiveForUser(Long userId) {
        final String sql = """
                    SELECT n.*, un.is_read
                    FROM tools_core.notifications n
                    JOIN tools_core.user_notifications un ON n.id = un.notification_id
                    WHERE un.user_id = ?
                    ORDER BY n.created_at DESC
                    LIMIT 50
                """;

        return jdbcTemplate.query(sql, NOTIFICATION_ROW_MAPPER, userId);
    }

    @Override
    public void markAsRead(Long userId, List<Long> notificationIds) {
        if (notificationIds == null || notificationIds.isEmpty()) {
            final String sql = "UPDATE tools_core.user_notifications SET is_read = TRUE, read_at = now() WHERE user_id = ? AND is_read = FALSE";
            jdbcTemplate.update(sql, userId);
        } else {
            final String sql = "UPDATE tools_core.user_notifications SET is_read = TRUE, read_at = now() WHERE user_id = ? AND notification_id = ANY(?)";
            jdbcTemplate.update(sql, userId, notificationIds.toArray(new Long[0]));
        }
    }

    @Override
    public void delete(Long userId, List<Long> notificationIds) {
        if (notificationIds == null || notificationIds.isEmpty()) {
            final String sql = "DELETE FROM tools_core.user_notifications WHERE user_id = ?";
            jdbcTemplate.update(sql, userId);
        } else {
            final String sql = "DELETE FROM tools_core.user_notifications WHERE user_id = ? AND notification_id = ANY(?)";
            jdbcTemplate.update(sql, userId, notificationIds.toArray(new Long[0]));
        }
    }
}

