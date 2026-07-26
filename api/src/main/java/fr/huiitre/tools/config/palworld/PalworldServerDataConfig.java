package fr.huiitre.tools.config.palworld;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.serverdata.application.ports.GuildQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.PalInstanceQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.PalLookupRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.ServerDataRepository;
import fr.huiitre.tools.modules.palworld.serverdata.infrastructure.PostgresGuildQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.infrastructure.PostgresPalInstanceQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.infrastructure.PostgresPalLookupRepository;
import fr.huiitre.tools.modules.palworld.serverdata.infrastructure.PostgresServerDataRepository;

@Configuration
public class PalworldServerDataConfig {

    @Bean
    public ServerDataRepository serverDataRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresServerDataRepository(jdbcTemplate);
    }

    @Bean
    public PalLookupRepository palLookupRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresPalLookupRepository(jdbcTemplate);
    }

    @Bean
    public GuildQueryRepository guildQueryRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresGuildQueryRepository(jdbcTemplate);
    }

    @Bean
    public PalInstanceQueryRepository palInstanceQueryRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresPalInstanceQueryRepository(jdbcTemplate);
    }
}
