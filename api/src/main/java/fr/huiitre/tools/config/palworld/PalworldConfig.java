package fr.huiitre.tools.config.palworld;

import com.fasterxml.jackson.databind.ObjectMapper;
import fr.huiitre.tools.modules.palworld.application.ports.PalworldServerPort;
import fr.huiitre.tools.modules.palworld.infrastructure.PalworldMockAdapter;
import fr.huiitre.tools.modules.palworld.infrastructure.PalworldRestAdapter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.MediaType;
import org.springframework.http.client.JdkClientHttpRequestFactory;
import org.springframework.http.converter.json.MappingJackson2HttpMessageConverter;
import org.springframework.web.client.RestTemplate;

import java.net.http.HttpClient;
import java.util.List;

@Configuration
public class PalworldConfig {

    @Bean
    public PalworldServerPort palworldServerPort(
            ObjectMapper objectMapper,
            @Value("${palworld.api.base-url}") String baseUrl,
            @Value("${palworld.api.mock:false}") boolean mock) {
        if (mock) {
            return new PalworldMockAdapter();
        }

        HttpClient httpClient = HttpClient.newBuilder()
                .version(HttpClient.Version.HTTP_1_1)
                .build();
        RestTemplate restTemplate = new RestTemplate(new JdkClientHttpRequestFactory(httpClient));

        // Le serveur Palworld répond en text/plain sur certains endpoints (ex: /v1/api/settings) malgré un corps JSON.
        // Ajouté en fin de liste (pas en position 0) pour ne pas passer devant ByteArrayHttpMessageConverter :
        // sinon Jackson sérialise nos body[] de POST en base64 au lieu des octets bruts (Content-Length devient faux).
        MappingJackson2HttpMessageConverter jsonConverter = new MappingJackson2HttpMessageConverter(objectMapper);
        jsonConverter.setSupportedMediaTypes(List.of(MediaType.APPLICATION_JSON, MediaType.TEXT_PLAIN));
        restTemplate.getMessageConverters().add(jsonConverter);

        return new PalworldRestAdapter(restTemplate, objectMapper, baseUrl);
    }
}
