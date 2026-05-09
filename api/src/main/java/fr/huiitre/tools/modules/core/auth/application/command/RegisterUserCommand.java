package fr.huiitre.tools.modules.core.auth.application.command;

import fr.huiitre.tools.modules.core.auth.domain.AuthProvider;

public class RegisterUserCommand {

    private final AuthProvider provider;

    // Identité commune
    private final String email;
    private final String name;
    private final String picture;

    // PASSWORD only
    private final String password;

    // OAUTH only
    private final String providerUserId;

    private RegisterUserCommand(
            AuthProvider provider,
            String email,
            String name,
            String picture,
            String password,
            String providerUserId) {

        this.provider = provider;
        this.email = email;
        this.name = name;
        this.picture = picture;
        this.password = password;
        this.providerUserId = providerUserId;
    }

    /*
     * =========================
     * FACTORY METHODS
     * =========================
     */

    public static RegisterUserCommand password(
            String email,
            String name,
            String password) {

        return new RegisterUserCommand(
                AuthProvider.PASSWORD,
                email,
                name,
                null,
                password,
                null);
    }

    public static RegisterUserCommand oauth(
            AuthProvider provider,
            String providerUserId,
            String picture,
            String email,
            String name) {

        if (provider == AuthProvider.PASSWORD) {
            throw new IllegalArgumentException("PASSWORD provider not allowed here");
        }

        return new RegisterUserCommand(
                provider,
                email,
                name,
                picture,
                null,
                providerUserId);
    }

    /*
     * =========================
     * GETTERS
     * =========================
     */

    public AuthProvider getProvider() {
        return provider;
    }

    public String getEmail() {
        return email;
    }

    public String getName() {
        return name;
    }

    public String getPicture() {
        return picture;
    }

    public String getPassword() {
        return password;
    }

    public String getProviderUserId() {
        return providerUserId;
    }

    /*
     * =========================
     * HELPERS
     * =========================
     */

    public boolean isPasswordAuth() {
        return provider == AuthProvider.PASSWORD;
    }

    public boolean isOAuthAuth() {
        return provider != AuthProvider.PASSWORD;
    }
}
