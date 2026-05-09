package fr.huiitre.tools.modules.core.auth.infrastructure.github;

import java.util.Map;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;
import org.springframework.web.reactive.function.client.WebClient;

@Component
public class GitHubOAuthClient {

    @Value("${GITHUB_CLIENT_ID}")
    private String clientId;

    @Value("${GITHUB_CLIENT_SECRET}")
    private String clientSecret;

    private final WebClient webClient;

    public GitHubOAuthClient(
            WebClient.Builder webClientBuilder) {
        this.webClient = webClientBuilder
                .baseUrl("https://github.com")
                .build();
        this.clientId = clientId;
        this.clientSecret = clientSecret;
    }

    /**
     * Échange le code OAuth GitHub contre un access_token
     */
    public String exchangeCodeForAccessToken(String code) {

        Map<String, Object> response = webClient
                .post()
                .uri("/login/oauth/access_token")
                .accept(MediaType.APPLICATION_JSON)
                .contentType(MediaType.APPLICATION_JSON)
                .bodyValue(Map.of(
                        "client_id", clientId,
                        "client_secret", clientSecret,
                        "code", code))
                .retrieve()
                .bodyToMono(Map.class)
                .block();

        if (response == null || !response.containsKey("access_token")) {
            throw new IllegalStateException("GITHUB_OAUTH_TOKEN_EXCHANGE_FAILED");
        }

        return response.get("access_token").toString();
    }
}
