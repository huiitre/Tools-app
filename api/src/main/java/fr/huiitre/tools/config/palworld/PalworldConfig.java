package fr.huiitre.tools.config.palworld;

import fr.huiitre.tools.modules.palworld.application.ports.PalworldServerPort;
import fr.huiitre.tools.modules.palworld.infrastructure.PalworldRestAdapter;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.client.RestTemplate;

@Configuration
public class PalworldConfig {

    @Bean
    public PalworldServerPort palworldServerPort() {
        return new PalworldRestAdapter(new RestTemplate());
    }
}
