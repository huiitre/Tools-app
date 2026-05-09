package fr.huiitre.tools.modules.core.auth.application.usecase;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.Mockito.*;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.NullAndEmptySource;
import org.junit.jupiter.params.provider.ValueSource;
import org.mockito.ArgumentCaptor;

import fr.huiitre.tools.modules.core.auth.application.command.RegisterUserCommand;
import fr.huiitre.tools.modules.core.auth.application.exception.RegisterException;
import fr.huiitre.tools.modules.core.auth.application.ports.PasswordHasher;
import fr.huiitre.tools.modules.core.auth.domain.AuthProvider;
import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.role.application.ports.UserRoleRepository;
import fr.huiitre.tools.modules.core.role.domain.Role;
import fr.huiitre.tools.modules.core.role.domain.UserRole;
import fr.huiitre.tools.modules.core.user.application.ports.UserAuthProviderRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserCredentialsRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.user.domain.AvatarSource;
import fr.huiitre.tools.modules.core.user.domain.User;
import fr.huiitre.tools.modules.core.user.domain.UserType;

import java.util.Optional;

@Tag("AUTH")
@DisplayName("Core - Authentification")
class RegisterUserUseCaseTest {

    @Nested
    @DisplayName("Validations")
    class Validations {

        private RegisterUserUseCase useCase;

        @BeforeEach
        void setUp() {
            useCase = new RegisterUserUseCase(
                    mock(UserRepository.class),
                    mock(UserCredentialsRepository.class),
                    mock(UserAuthProviderRepository.class),
                    mock(UserRoleRepository.class),
                    mock(RoleRepository.class),
                    mock(PasswordHasher.class));
        }

        @ParameterizedTest
        @NullAndEmptySource
        @ValueSource(strings = { "   " })
        @DisplayName("Devrait lever une RegisterException si le nom est invalide")
        void should_throw_when_name_is_invalid(String invalidName) {

            RegisterUserCommand command = RegisterUserCommand.password(
                    "a@a.fr",
                    invalidName,
                    "password123");

            assertThrows(
                    RegisterException.class,
                    () -> useCase.execute(command));
        }

        @ParameterizedTest
        @NullAndEmptySource
        @ValueSource(strings = { "   " })
        @DisplayName("Devrait lever une RegisterException si l'email est invalide")
        void should_throw_when_email_is_invalid(String invalidMail) {

            RegisterUserCommand command = RegisterUserCommand.password(
                    invalidMail,
                    "yanis",
                    "password123");

            assertThrows(
                    RegisterException.class,
                    () -> useCase.execute(command));
        }

        @ParameterizedTest
        @NullAndEmptySource
        @ValueSource(strings = { "   " })
        @DisplayName("Devrait lever une RegisterException si le mot de passe est invalide")
        void should_throw_when_password_is_invalid(String invalidPassword) {

            RegisterUserCommand command = RegisterUserCommand.password(
                    "a@a.fr",
                    "yanis",
                    invalidPassword);

            assertThrows(
                    RegisterException.class,
                    () -> useCase.execute(command));
        }
    }

    @Nested
    @DisplayName("Règles métier")
    class BusinessRules {

        private UserRepository userRepository;
        private UserCredentialsRepository userCredentialsRepository;
        private UserAuthProviderRepository userAuthProviderRepository;
        private UserRoleRepository userRoleRepository;
        private RoleRepository roleRepository;
        private PasswordHasher passwordHasher;

        private RegisterUserUseCase usecase;

        @BeforeEach
        void setUp() {
            userRepository = mock(UserRepository.class);
            userCredentialsRepository = mock(UserCredentialsRepository.class);
            userAuthProviderRepository = mock(UserAuthProviderRepository.class);
            userRoleRepository = mock(UserRoleRepository.class);
            roleRepository = mock(RoleRepository.class);
            passwordHasher = mock(PasswordHasher.class);

            usecase = new RegisterUserUseCase(
                    userRepository,
                    userCredentialsRepository,
                    userAuthProviderRepository,
                    userRoleRepository,
                    roleRepository,
                    passwordHasher);
        }

        @Test
        @DisplayName("Devrait lever une RegisterException si l'email existe déjà et que l'utilisateur est actif")
        void should_throw_when_email_exists_and_user_is_active() {

            User activeUser = mock(User.class);

            when(activeUser.isActive()).thenReturn(true);
            when(userRepository.findByEmail("a@a.fr")).thenReturn(Optional.of(activeUser));

            RegisterUserCommand command = RegisterUserCommand.password(
                    "a@a.fr",
                    "Yanis",
                    "password123");

            assertThrows(RegisterException.class, () -> usecase.execute(command));
        }

        @Test
        @DisplayName("Devrait retourner l'utilisateur existant si l'email existe et que l'utilisateur est inactif")
        void should_return_existing_user_when_email_exists_and_user_is_inactive() {

            User inactiveUser = mock(User.class);

            when(inactiveUser.isActive()).thenReturn(false);
            when(userRepository.findByEmail("a@a.fr")).thenReturn(Optional.of(inactiveUser));

            RegisterUserCommand command = RegisterUserCommand.password(
                    "a@a.fr",
                    "Yanis",
                    "password123");

            assertSame(inactiveUser, usecase.execute(command));
        }

        @Test
        @DisplayName("Devrait lever une RegisterException si le rôle USER est introuvable")
        void should_throw_when_user_role_is_missing() {

            when(roleRepository.findByCode("USER")).thenReturn(Optional.empty());

            RegisterUserCommand command = RegisterUserCommand.password(
                    "a@a.fr",
                    "Yanis",
                    "password123");

            assertThrows(RegisterException.class, () -> usecase.execute(command));
        }
    }

    @Nested
    @DisplayName("Cas nominal")
    class NominalCase {

        private UserRepository userRepository;
        private UserCredentialsRepository userCredentialsRepository;
        private UserAuthProviderRepository userAuthProviderRepository;
        private UserRoleRepository userRoleRepository;
        private RoleRepository roleRepository;
        private PasswordHasher passwordHasher;

        private RegisterUserUseCase usecase;

        @BeforeEach
        void setUp() {
            userRepository = mock(UserRepository.class);
            userCredentialsRepository = mock(UserCredentialsRepository.class);
            userAuthProviderRepository = mock(UserAuthProviderRepository.class);
            userRoleRepository = mock(UserRoleRepository.class);
            roleRepository = mock(RoleRepository.class);
            passwordHasher = mock(PasswordHasher.class);

            usecase = new RegisterUserUseCase(
                    userRepository,
                    userCredentialsRepository,
                    userAuthProviderRepository,
                    userRoleRepository,
                    roleRepository,
                    passwordHasher);
        }

        @Test
        @DisplayName("Devrait créer un utilisateur complet lorsque toutes les conditions sont valides")
        void should_create_user_when_command_is_valid() {

            //* GIVEN */
            Role role = mock(Role.class);

            RegisterUserCommand command = RegisterUserCommand.password(
                "a@a.fr",
                "Yanis",
                "password123"
            );

            //* WHEN */
            when(roleRepository.findByCode("USER")).thenReturn(Optional.of(role));
            when(userRepository.findByEmail(command.getEmail())).thenReturn(Optional.empty());
            when(passwordHasher.hash(command.getPassword())).thenReturn("HASHED");

            usecase.execute(command);

            //* THEN */
            ArgumentCaptor<User> userCaptor = ArgumentCaptor.forClass(User.class);
            verify(userRepository).save(userCaptor.capture());
            User savedUser = userCaptor.getValue();

            assertEquals(command.getName(), savedUser.getName());
            assertEquals(command.getEmail(), savedUser.getEmail());
            assertEquals(UserType.HUMAN, savedUser.getUserType());
            assertEquals(AvatarSource.PASSWORD, savedUser.getAvatarSource());

            verify(passwordHasher, times(1)).hash(command.getPassword());
            verify(userCredentialsRepository, times(1)).save(any(), eq("HASHED"));
            verify(userAuthProviderRepository, times(1)).save(
                any(),
                eq(AuthProvider.PASSWORD),
                eq(command.getEmail()),
                eq(command.getEmail()),
                isNull()
            );
            verify(userRoleRepository, times(1)).save(any(UserRole.class));
        }
    }
}
