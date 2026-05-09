package fr.huiitre.tools.modules.core.user.infrastructure;

import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.core.user.application.ports.UserAuthProviderRepository;
import fr.huiitre.tools.modules.core.auth.domain.AuthProvider;

public class PostgresUserAuthProviderRepository implements UserAuthProviderRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresUserAuthProviderRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public boolean existsByUserIdAndProvider(Long userId, AuthProvider provider) {
        final String sql = """
                    SELECT EXISTS (
                        SELECT 1
                        FROM tools_core.user_auth_provider
                        WHERE user_id = ? AND provider = ?
                    )
                """;

        return Boolean.TRUE.equals(
                jdbcTemplate.queryForObject(
                        sql,
                        Boolean.class,
                        userId,
                        provider.name()));
    }

    @Override
    public boolean existsByProviderAndProviderUserId(
            AuthProvider provider,
            String providerUserId) {

        final String sql = """
                    SELECT EXISTS (
                        SELECT 1
                        FROM tools_core.user_auth_provider
                        WHERE provider = ? AND provider_user_id = ?
                    )
                """;

        return Boolean.TRUE.equals(
                jdbcTemplate.queryForObject(
                        sql,
                        Boolean.class,
                        provider.name(),
                        providerUserId));
    }

    @Override
    public void save(
            Long userId,
            AuthProvider provider,
            String providerUserId,
            String providerEmail,
            String providerAvatarUrl) {

        final String sql = """
                    INSERT INTO tools_core.user_auth_provider
                        (user_id, provider, provider_user_id, provider_email, provider_avatar_url)
                    VALUES (?, ?, ?, ?, ?)
                """;

        jdbcTemplate.update(
                sql,
                userId,
                provider.name(),
                providerUserId,
                providerEmail,
                providerAvatarUrl);
    }

    @Override
    public Optional<Long> findUserIdByProviderAndProviderUserId(
            AuthProvider provider,
            String providerUserId) {

        final String sql = """
                    SELECT user_id
                    FROM tools_core.user_auth_provider
                    WHERE provider = ? AND provider_user_id = ?
                    LIMIT 1
                """;

        return jdbcTemplate.query(
                sql,
                rs -> rs.next() ? Optional.of(rs.getLong("user_id")) : Optional.empty(),
                provider.name(),
                providerUserId);
    }

    @Override
    public Optional<String> findProviderAvatarUrl(Long userId, AuthProvider provider) {
        final String sql = """
                    SELECT provider_avatar_url
                    FROM tools_core.user_auth_provider
                    WHERE user_id = ? AND provider = ?
                    LIMIT 1
                """;

        return jdbcTemplate.query(
                sql,
                rs -> rs.next() ? Optional.ofNullable(rs.getString("provider_avatar_url")) : Optional.empty(),
                userId,
                provider.name());
    }

    @Override
    public void updateProviderAvatarUrl(
            Long userId,
            AuthProvider provider,
            String newAvatarUrl) {
        final String sql = """
                    UPDATE tools_core.user_auth_provider
                    SET provider_avatar_url = ?
                    WHERE user_id = ? AND provider = ?
                """;

        jdbcTemplate.update(
                sql,
                newAvatarUrl,
                userId,
                provider.name());
    }
}