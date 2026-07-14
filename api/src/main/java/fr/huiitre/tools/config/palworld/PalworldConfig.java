package fr.huiitre.tools.config.palworld;

import fr.huiitre.tools.modules.palworld.application.ports.PalworldServerPort;
import fr.huiitre.tools.modules.palworld.infrastructure.PalworldRestAdapter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.client.JdkClientHttpRequestFactory;
import org.springframework.web.client.RestTemplate;

import java.net.http.HttpClient;

@Configuration
public class PalworldConfig {

    @Bean
    public PalworldServerPort palworldServerPort(@Value("${palworld.api.base-url}") String baseUrl) {
        HttpClient httpClient = HttpClient.newBuilder()
                .version(HttpClient.Version.HTTP_1_1)
                .build();
        RestTemplate restTemplate = new RestTemplate(new JdkClientHttpRequestFactory(httpClient));
        return new PalworldRestAdapter(restTemplate, baseUrl);
    }
}
