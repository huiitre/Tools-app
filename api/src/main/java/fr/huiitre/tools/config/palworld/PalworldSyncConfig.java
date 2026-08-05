package fr.huiitre.tools.config.palworld;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.sync.application.ports.BreedingExceptionDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.BreedingExceptionSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ElementDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ElementSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalworldLanguageDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.SkillDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.SkillSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkPriorityDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkPrioritySyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkSuitabilityDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkSuitabilitySyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalAssetsReader;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalBreedingExceptionDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalElementDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalLanguageDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalPalDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalSkillDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalWorkPriorityDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalWorkSuitabilityDataProvider;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresBreedingExceptionSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresElementSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresPalSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresSkillSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresWorkPrioritySyncRepository;
import fr.huiitre.tools.modules.palworld.sync.infrastructure.PostgresWorkSuitabilitySyncRepository;

@Configuration
public class PalworldSyncConfig {

    @Bean
    public PalworldLanguageDataProvider palworldLanguageDataProvider(PalworldLocalAssetsReader assetsReader) {
        return new PalworldLocalLanguageDataProvider(assetsReader);
    }

    @Bean
    public ElementDataProvider elementDataProvider(
            PalworldLocalAssetsReader assetsReader,
            PalworldLanguageDataProvider languageDataProvider,
            @Value("${app.assets.base-url}") String assetsBaseUrl) {
        return new PalworldLocalElementDataProvider(assetsReader, languageDataProvider, assetsBaseUrl);
    }

    @Bean
    public ElementSyncRepository elementSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresElementSyncRepository(jdbcTemplate);
    }

    @Bean
    public WorkSuitabilityDataProvider workSuitabilityDataProvider(
            PalworldLocalAssetsReader assetsReader,
            PalworldLanguageDataProvider languageDataProvider,
            @Value("${app.assets.base-url}") String assetsBaseUrl) {
        return new PalworldLocalWorkSuitabilityDataProvider(assetsReader, languageDataProvider, assetsBaseUrl);
    }

    @Bean
    public WorkSuitabilitySyncRepository workSuitabilitySyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresWorkSuitabilitySyncRepository(jdbcTemplate);
    }

    @Bean
    public WorkPriorityDataProvider workPriorityDataProvider(
            PalworldLocalAssetsReader assetsReader, PalworldLanguageDataProvider languageDataProvider) {
        return new PalworldLocalWorkPriorityDataProvider(assetsReader, languageDataProvider);
    }

    @Bean
    public WorkPrioritySyncRepository workPrioritySyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresWorkPrioritySyncRepository(jdbcTemplate);
    }

    @Bean
    public SkillDataProvider skillDataProvider(
            PalworldLocalAssetsReader assetsReader, PalworldLanguageDataProvider languageDataProvider) {
        return new PalworldLocalSkillDataProvider(assetsReader, languageDataProvider);
    }

    @Bean
    public SkillSyncRepository skillSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresSkillSyncRepository(jdbcTemplate);
    }

    @Bean
    public PalDataProvider palDataProvider(
            PalworldLocalAssetsReader assetsReader,
            PalworldLanguageDataProvider languageDataProvider,
            @Value("${app.assets.base-url}") String assetsBaseUrl) {
        return new PalworldLocalPalDataProvider(assetsReader, languageDataProvider, assetsBaseUrl);
    }

    @Bean
    public PalSyncRepository palSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresPalSyncRepository(jdbcTemplate);
    }

    @Bean
    public BreedingExceptionDataProvider breedingExceptionDataProvider(PalworldLocalAssetsReader assetsReader) {
        return new PalworldLocalBreedingExceptionDataProvider(assetsReader);
    }

    @Bean
    public BreedingExceptionSyncRepository breedingExceptionSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresBreedingExceptionSyncRepository(jdbcTemplate);
    }
}
