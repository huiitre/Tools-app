package fr.huiitre.tools.modules.core.security.infrastructure;

import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;

@Component
public class SpringAuthenticatedUserProvider
        implements AuthenticatedUserProvider {

    @Override
    public Long getUserId() {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();

        if (authentication == null || !authentication.isAuthenticated()) {
            throw new IllegalStateException("No authenticated user");
        }

        Object principal = authentication.getPrincipal();

        if (principal instanceof String userId) {
            return Long.valueOf(userId);
        }

        throw new IllegalStateException(
                "Unsupported principal type: " + principal.getClass().getName());
    }
}
