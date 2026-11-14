package fr.huiitre.tools.modules.dofus.pricing.infrastructure;

import java.util.List;
import java.util.Optional;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.dofus.pricing.application.ports.CaptureAccountRepository;

public class PostgresCaptureAccountRepository implements CaptureAccountRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresCaptureAccountRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public Optional<Long> findUserIdByEmail(String email) {
        List<Long> ids = jdbcTemplate.queryForList(
            "SELECT id FROM tools_core.users WHERE lower(email) = lower(?)",
            Long.class,
            email);
        return ids.stream().findFirst();
    }
}
