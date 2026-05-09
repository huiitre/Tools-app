package fr.huiitre.tools.modules.core.auth.infrastructure.github;

public record GitHubUser(
        String providerUserId,
        String email,
        String name) {
}