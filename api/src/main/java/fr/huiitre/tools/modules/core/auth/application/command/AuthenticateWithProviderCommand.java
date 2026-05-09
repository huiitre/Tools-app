package fr.huiitre.tools.modules.core.auth.application.command;

import fr.huiitre.tools.modules.core.auth.domain.AuthProvider;

public class AuthenticateWithProviderCommand {
    private final AuthProvider provider;
    private final String providerUserId;
    private final String email;
    private final String name;

    public AuthenticateWithProviderCommand(
            AuthProvider provider,
            String providerUserId,
            String email,
            String name) {
        this.provider = provider;
        this.providerUserId = providerUserId;
        this.email = email;
        this.name = name;
    }

    public AuthProvider getProvider() {
        return provider;
    }

    public String getProviderUserId() {
        return providerUserId;
    }

    public String getEmail() {
        return email;
    }

    public String getName() {
        return name;
    }
}
