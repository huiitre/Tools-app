package fr.huiitre.tools.modules.core.security.infrastructure;

import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;

import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;

public class SpringSecurityCurrentUserProvider implements CurrentUserProvider {

    @Override
    public String getCurrentUserId() {

        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();

        if (authentication == null || !authentication.isAuthenticated()) {
            throw new IllegalStateException("No authenticated user");
        }

        return authentication.getName(); // JWT sub
    }
}
