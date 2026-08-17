package fr.huiitre.tools.modules.core.notification.infrastructure.persistence;

import java.util.List;

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

