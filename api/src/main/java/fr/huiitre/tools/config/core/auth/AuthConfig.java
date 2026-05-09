package fr.huiitre.tools.config.core.auth;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.mail.javamail.JavaMailSender;

import fr.huiitre.tools.modules.core.auth.application.ports.EmailSender;
import fr.huiitre.tools.modules.core.auth.application.ports.PasswordHasher;
import fr.huiitre.tools.modules.core.auth.application.ports.UserEmailVerificationRepository;
import fr.huiitre.tools.modules.core.auth.application.ports.UserPasswordResetRepository;
import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.role.application.ports.UserRoleRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserAuthProviderRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserCredentialsRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.auth.application.usecase.RegisterUserUseCase;
import fr.huiitre.tools.modules.core.auth.infrastructure.mail.AuthMailSenderService;
import fr.huiitre.tools.modules.core.auth.infrastructure.password.BCryptPasswordHasher;
import fr.huiitre.tools.modules.core.user.infrastructure.PostgresUserAuthProviderRepository;
import fr.huiitre.tools.modules.core.user.infrastructure.PostgresUserCredentialsRepository;
import fr.huiitre.tools.modules.core.user.infrastructure.PostgresUserEmailVerificationRepository;
import fr.huiitre.tools.modules.core.user.infrastructure.PostgresUserPasswordResetRepository;
import fr.huiitre.tools.modules.core.user.infrastructure.PostgresUserRepository;

@Configuration
public class AuthConfig {

    @Bean
    public UserRepository userRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresUserRepository(jdbcTemplate);
    }

    @Bean
    public UserCredentialsRepository userCredentialsRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresUserCredentialsRepository(jdbcTemplate);
    }

    @Bean
    public UserAuthProviderRepository userAuthProviderRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresUserAuthProviderRepository(jdbcTemplate);
    }

    @Bean
    public PasswordHasher passwordHasher() {
        return new BCryptPasswordHasher();
    }

    @Bean
    public RegisterUserUseCase registerUserUseCase(
            UserRepository userRepository,
            UserCredentialsRepository userCredentialsRepository,
            UserAuthProviderRepository userAuthProviderRepository,
            UserRoleRepository userRoleRepository,
            RoleRepository roleRepository,
            PasswordHasher passwordHasher) {
        return new RegisterUserUseCase(
                userRepository,
                userCredentialsRepository,
                userAuthProviderRepository,
                userRoleRepository,
                roleRepository,
                passwordHasher);
    }

    @Bean
    public UserEmailVerificationRepository userEmailVerificationRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresUserEmailVerificationRepository(jdbcTemplate);
    }

    @Bean
    public EmailSender emailSender(
            JavaMailSender mailSender) {
        return new AuthMailSenderService(mailSender);
    }

    @Bean
    public UserPasswordResetRepository userPasswordResetRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresUserPasswordResetRepository(jdbcTemplate);
    }
}
