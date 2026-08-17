package fr.huiitre.tools.config.security;

import javax.sql.DataSource;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.ports.ModuleAuthorizationPort;
import fr.huiitre.tools.modules.core.security.application.ports.UserRoleProvider;
import fr.huiitre.tools.modules.core.security.infrastructure.aop.UseCaseAuthorizationAspect;
import fr.huiitre.tools.modules.core.security.infrastructure.PostgresModuleAuthorizationAdapter;
import fr.huiitre.tools.modules.core.security.infrastructure.PostgresUserRoleProvider;
import fr.huiitre.tools.modules.core.security.infrastructure.SpringSecurityCurrentUserProvider;

@Configuration
public class AuthorizationConfig {

    @Bean
    public ModuleAuthorizationPort moduleAuthorizationPort(DataSource dataSource) {
        // return new FakeModuleAuthorizationAdapter();
        return new PostgresModuleAuthorizationAdapter(dataSource);
    }

    @Bean
    public CurrentUserProvider currentUserProvider() {
        return new SpringSecurityCurrentUserProvider();
    }

    @Bean
    public UseCaseAuthorizationAspect useCaseAuthorizationAspect(
            ModuleAuthorizationPort moduleAuthorizationPort,
            UserRoleProvider userRoleProvider,
            CurrentUserProvider currentUserProvider) {
        return new UseCaseAuthorizationAspect(
                moduleAuthorizationPort,
                userRoleProvider,
                currentUserProvider);
    }

    @Bean
    public UserRoleProvider userRoleProvider(DataSource dataSource) {
        return new PostgresUserRoleProvider(dataSource);
    }
}