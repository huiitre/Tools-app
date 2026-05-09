package fr.huiitre.tools.modules.core.user.domain;

import static org.junit.jupiter.api.Assertions.*;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

@Tag("USER")
@DisplayName("Core - User")
class UserTest {

    @Nested
    @DisplayName("Initialisation")
    class Initialize {

        /**
         * Initialise active à false
         */
        @Test
        @DisplayName("Doit initialiser avec active à false")
        void should_initialize_active_to_false() {

            // * given */
            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    UserType.HUMAN,
                    AvatarSource.PASSWORD);

            // * when */
            // * then */
            assertFalse(user.isActive());
        }

        @Test
        @DisplayName("Doit conserver le nom")
        void should_conserve_name_when_initialize() {

            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    UserType.HUMAN,
                    AvatarSource.PASSWORD);

            assertEquals("Yanis", user.getName());
        }

        @Test
        @DisplayName("Doit conserver l'email")
        void should_conserve_email_when_initialize() {

            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    UserType.HUMAN,
                    AvatarSource.PASSWORD);

            assertEquals("a@a.fr", user.getEmail());
        }

        @Test
        @DisplayName("Doit conserver le type utilisateur")
        void should_conserve_usertype_when_initialize() {

            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    UserType.SYSTEM,
                    AvatarSource.GOOGLE);

            assertEquals(UserType.SYSTEM, user.getUserType());
        }

        @Test
        @DisplayName("Doit conserver l'avatar source")
        void should_conserve_avatarsource_when_initialize() {

            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    UserType.HUMAN,
                    AvatarSource.GOOGLE);

            assertEquals(AvatarSource.GOOGLE, user.getAvatarSource());
        }

        @Test
        @DisplayName("Doit renseigner l'AvatarSource par défaut sur PASSWORD si non renseigné")
        void should_have_default_avatarsource_if_empty() {

            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    UserType.HUMAN,
                    null);

            assertEquals(AvatarSource.PASSWORD, user.getAvatarSource());
        }

        @Test
        @DisplayName("Doit renseigner le type utilisateur par défaut sur HUMAN si non renseigné")
        void shoukd_have_default_usertype_if_empty() {

            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    null,
                    null);

            assertEquals(UserType.HUMAN, user.getUserType());
        }
    }

    @Nested
    @DisplayName("Activation")
    class Activation {

        @Test
        @DisplayName("setIsActive(true) met bien active à true")
        void should_set_active_to_true_when_setIsActive_true() {
            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    null,
                    null);
            user.setIsActive(true);

            assertTrue(user.isActive());
        }

        @Test
        @DisplayName("setIsActive(false) met bien active à false")
        void should_set_active_to_false_when_setIsActive_false() {
            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    null,
                    null);
            user.setIsActive(true);
            user.setIsActive(false);

            assertFalse(user.isActive());
        }
    }

    @Nested
    @DisplayName("AvatarSource")
    class AvatarSourceTest {

        @Test
        @DisplayName("setAvatarSource(null) remet la valeur par défaut PASSWORD")
        void should_set_avatarSource_to_PASSWORD_when_null_is_provided() {

            User user = new User(
                    "Yanis",
                    "a@a.fr",
                    null,
                    AvatarSource.GOOGLE);

            user.setAvatarSource(null);

            assertEquals(AvatarSource.PASSWORD, user.getAvatarSource());
        }

        @Test
        @DisplayName("setAvatarSource(GOOGLE) converse GOOGLE")
        void should_keep_avatarSource_when_non_null_is_provided() {

            User user = new User(
                "Yanis",
                "a@a.fr",
                null,
                AvatarSource.PASSWORD
            );

            user.setAvatarSource(AvatarSource.GOOGLE);

            assertEquals(AvatarSource.GOOGLE, user.getAvatarSource());
        }
    }

    @Nested
    @DisplayName("Identité")
    class Identify {

        @Test
        @DisplayName("setId() puis getId() retourne bien l'id donné")
        void should_return_same_id_after_setId() {

            User user = new User(
                "Yanis",
                "a@a.fr",
                null,
                AvatarSource.PASSWORD
            );

            user.setId(10L);

            assertEquals(10L, user.getId());
        }
    }
}
