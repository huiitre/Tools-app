package fr.huiitre.tools.modules.dofus.almanax.infrastructure;

import java.time.LocalDate;
import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxSubscriptionRepository;

public class PostgresAlmanaxSubscriptionRepository implements AlmanaxSubscriptionRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresAlmanaxSubscriptionRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void add(Long userId, Long almanaxId, LocalDate date) {
        final String sql = """
                INSERT INTO tools_dofus.almanax_subscription (user_id, almanax_id, date)
                VALUES (?, ?, ?)
                """;
        jdbcTemplate.update(sql, userId, almanaxId, date);
    }

    @Override
    public void remove(Long userId, Long almanaxId) {
        final String sql = """
                DELETE FROM tools_dofus.almanax_subscription
                WHERE user_id = ? AND almanax_id = ?
                """;
        int affected = jdbcTemplate.update(sql, userId, almanaxId);
        if (affected == 0) {
            throw new IllegalArgumentException("ALMANAX_SUBSCRIPTION_NOT_FOUND");
        }
    }

    @Override
    public boolean existsByUserIdAndAlmanaxId(Long userId, Long almanaxId) {
        final String sql = """
                SELECT COUNT(*)
                FROM tools_dofus.almanax_subscription
                WHERE user_id = ? AND almanax_id = ?
                """;
        Integer count = jdbcTemplate.queryForObject(sql, Integer.class, userId, almanaxId);
        return count != null && count > 0;
    }

    @Override
    public List<Long> findAlmanaxIdsByUserId(Long userId) {
        final String sql = """
                SELECT almanax_id
                FROM tools_dofus.almanax_subscription
                WHERE user_id = ?
                """;
        return jdbcTemplate.queryForList(sql, Long.class, userId);
    }

    @Override
    public List<Long> findUserIdsByAlmanaxId(Long almanaxId) {
        final String sql = """
                SELECT user_id
                FROM tools_dofus.almanax_subscription
                WHERE almanax_id = ?
                """;
        return jdbcTemplate.queryForList(sql, Long.class, almanaxId);
    }
}
