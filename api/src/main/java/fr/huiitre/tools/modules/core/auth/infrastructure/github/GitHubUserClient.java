package fr.huiitre.tools.modules.core.auth.infrastructure.github;

import java.util.List;
import java.util.Map;

import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;
import org.springframework.web.reactive.function.client.WebClient;

@Component
public class GitHubUserClient {

    private final WebClient webClient;

    public GitHubUserClient(WebClient.Builder webClientBuilder) {
        this.webClient = webClientBuilder
                .baseUrl("https://api.github.com")
                .defaultHeader("Accept", MediaType.APPLICATION_JSON_VALUE)
                .build();
    }

    /**
     * Récupère les infos utilisateur GitHub à partir de l'access token
     */
    public GitHubUser fetchUser(String accessToken) {

        Map<String, Object> user = webClient
                .get()
                .uri("/user")
                .headers(h -> h.setBearerAuth(accessToken))
                .retrieve()
                .bodyToMono(Map.class)
                .block();

        if (user == null || !user.containsKey("id")) {
            throw new IllegalStateException("GITHUB_USER_FETCH_FAILED");
        }

        String email = (String) user.get("email");
        if (email == null) {
            email = fetchPrimaryEmail(accessToken);
        }

        return new GitHubUser(
                user.get("id").toString(),
                email,
                (String) user.getOrDefault("name", user.get("login")));
    }

    /**
     * Fallback email via /user/emails
     */
    private String fetchPrimaryEmail(String accessToken) {

        List<Map<String, Object>> emails = webClient
                .get()
                .uri("/user/emails")
                .headers(h -> h.setBearerAuth(accessToken))
                .retrieve()
                .bodyToMono(List.class)
                .block();

        if (emails == null) {
            throw new IllegalStateException("GITHUB_EMAIL_FETCH_FAILED");
        }

        return emails.stream()
                .filter(e -> Boolean.TRUE.equals(e.get("primary")))
                .map(e -> (String) e.get("email"))
                .findFirst()
                .orElseThrow(() -> new IllegalStateException("GITHUB_PRIMARY_EMAIL_NOT_FOUND"));
    }
}
