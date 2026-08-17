package fr.huiitre.tools.config.elite_dangerous;

import java.util.List;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.elite_dangerous.r2r.application.importer.RouteImporter;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.ports.R2rExpeditionRepository;
import fr.huiitre.tools.modules.elite_dangerous.r2r.infrastructure.PostgresR2rExpeditionRepository;
import fr.huiitre.tools.modules.elite_dangerous.r2r.infrastructure.importer.SpanshJsonRouteImporter;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase.ImportR2rExpeditionUseCase;

@Configuration
public class EliteDangerousConfig {

    @Bean
    public R2rExpeditionRepository r2rExpeditionRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresR2rExpeditionRepository(jdbcTemplate);
    }

    @Bean
    public SpanshJsonRouteImporter spanshJsonRouteImporter() {
        return new SpanshJsonRouteImporter();
    }

    @Bean
    public ImportR2rExpeditionUseCase importR2rExpeditionUseCase(
            List<RouteImporter> importers,
            R2rExpeditionRepository repository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        return new ImportR2rExpeditionUseCase(importers, repository, authenticatedUserProvider);
    }
}
