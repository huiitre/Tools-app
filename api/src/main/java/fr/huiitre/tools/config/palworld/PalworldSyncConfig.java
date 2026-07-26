package fr.huiitre.tools.config.palworld;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.sync.application.ports.ElementDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ElementSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.ports.SkillDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.SkillSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkSuitabilityDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkSuitabilitySyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalAssetsReader;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalElementDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalPalDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalSkillDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalWorkSuitabilityDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresElementSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresPalSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresSkillSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresWorkSuitabilitySyncRepository;

@Configuration
public class PalworldSyncConfig {

    @Bean
    public ElementDataProvider elementDataProvider(PalworldLocalAssetsReader assetsReader) {
        return new PalworldLocalElementDataProvider(assetsReader);
    }

    @Bean
    public ElementSyncRepository elementSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresElementSyncRepository(jdbcTemplate);
    }

    @Bean
    public WorkSuitabilityDataProvider workSuitabilityDataProvider(PalworldLocalAssetsReader assetsReader) {
        return new PalworldLocalWorkSuitabilityDataProvider(assetsReader);
    }

    @Bean
    public WorkSuitabilitySyncRepository workSuitabilitySyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresWorkSuitabilitySyncRepository(jdbcTemplate);
    }

    @Bean
    public SkillDataProvider skillDataProvider(PalworldLocalAssetsReader assetsReader) {
        return new PalworldLocalSkillDataProvider(assetsReader);
    }

    @Bean
    public SkillSyncRepository skillSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresSkillSyncRepository(jdbcTemplate);
    }

    @Bean
    public PalDataProvider palDataProvider(PalworldLocalAssetsReader assetsReader) {
        return new PalworldLocalPalDataProvider(assetsReader);
    }

    @Bean
    public PalSyncRepository palSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresPalSyncRepository(jdbcTemplate);
    }
}
