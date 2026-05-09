package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.ports.RiotAuthPort;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantTokenView;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.*;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.RestTemplate;

import java.util.Map;

public class RiotAuthHttpAdapter implements RiotAuthPort {

    private static final String TOKEN_URL = "https://auth.riotgames.com/token";
    private static final String CLIENT_ID = "prod-xsso-playvalorant";

    private final RestTemplate restTemplate;

    public RiotAuthHttpAdapter(RestTemplate restTemplate) {
        this.restTemplate = restTemplate;
    }

    @Override
    public ValorantTokenView refresh(String refreshToken) {
        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);

        MultiValueMap<String, String> body = new LinkedMultiValueMap<>();
        body.add("grant_type", "refresh_token");
        body.add("refresh_token", refreshToken);
        body.add("client_id", CLIENT_ID);

        HttpEntity<MultiValueMap<String, String>> request = new HttpEntity<>(body, headers);

        try {
            ResponseEntity<Map<String, Object>> response = restTemplate.exchange(
                    TOKEN_URL, HttpMethod.POST, request,
                    new ParameterizedTypeReference<>() {});

            Map<String, Object> responseBody = response.getBody();
            if (responseBody == null) {
                throw new IllegalArgumentException("RIOT_AUTH_EMPTY_RESPONSE");
            }

            return new ValorantTokenView(
                    (String) responseBody.get("access_token"),
                    (String) responseBody.get("refresh_token"));

        } catch (HttpClientErrorException e) {
            throw new IllegalArgumentException("RIOT_TOKEN_INVALID");
        }
    }
}
