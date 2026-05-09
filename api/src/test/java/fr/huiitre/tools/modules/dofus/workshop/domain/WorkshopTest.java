package fr.huiitre.tools.modules.dofus.workshop.domain;

import static org.junit.jupiter.api.Assertions.*;

import java.util.List;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.NullAndEmptySource;
import org.junit.jupiter.params.provider.ValueSource;

@Tag("DOFUS WORKSHOP")
@DisplayName("Dofus - Workshop")
class WorkshopTest {

    @Nested
    @DisplayName("Création")
    class Creation {

        @ParameterizedTest
        @ValueSource(strings = { "Workshop name 1" })
        @DisplayName("Vérifier que la création avec un nom valide initialise correctement les valeurs par défaut")
        void create_with_valid_name_should_initialize_default_values(String validName) {
            
            Workshop workshop = Workshop.create(validName);

            assertTrue(workshop.isActive());
            assertFalse(workshop.isPinned());

            assertEquals(validName, workshop.getName());
        }

        @ParameterizedTest
        @NullAndEmptySource
        @ValueSource(strings = { "   ", "Nom invalide avec plus de 30 caractères" })
        @DisplayName("Vérifier que la création avec un nom invalide lève une exception")
        void create_with_invalid_name_should_throw_exception(String invalidName) {

            assertThrows(IllegalArgumentException.class, () -> Workshop.create(invalidName));
        }
    }

    @Nested
    @DisplayName("Modification")
    class Update {

        @ParameterizedTest
        @ValueSource(strings = { "Nom du workshop modifié" })
        @DisplayName("Vérifier que la mise à jour avec un nom valide modifie correctement l’état")
        void update_with_valid_name_should_modify_state(String validName) {

            Workshop workshop = Workshop.create("Nom de création");

            workshop.update(
                validName,
                false,
                true
            );

            assertEquals(validName, workshop.getName());
            assertFalse(workshop.isActive());
            assertTrue(workshop.isPinned());
        }

        @ParameterizedTest
        @NullAndEmptySource
        @ValueSource(strings = { "   ", "Nom invalide avec plus de 30 catactères" })
        @DisplayName("Vérifier que la mise à jour avec un nom invalide lève une exception")
        void update_with_invalid_name_should_throw_exception(String invalidName) {

            Workshop workshop = Workshop.create("Nom de création");

            assertThrows(
                IllegalArgumentException.class,
                () -> workshop.update(
                    invalidName,
                    workshop.isActive(),
                    workshop.isPinned()
                )
            );
        }
    }

    @Nested
    @DisplayName("Réhydratation")
    class Rehydrate {

        @ParameterizedTest
        @ValueSource(strings = { "Nom réhydraté" })
        @DisplayName("Vérifier que la reconstitution avec des données valides reconstruit correctement l’objet")
        void rehydrate_with_valid_data_should_reconstruct_workshop(String validName) {

            Workshop workshop = Workshop.rehydrate(10L, validName, true, true, List.of());

            assertEquals(validName, workshop.getName());
            assertTrue(workshop.isActive());
            assertTrue(workshop.isPinned());
        }

        @ParameterizedTest
        @ValueSource(strings = { "    ", "Nom invalide réhydraté avec plus de 30 caractères" })
        @NullAndEmptySource
        @DisplayName("Vérifier que la reconstitution avec un nom invalide lève une exception")
        void rehydrate_with_invalid_name_should_throw_exception(String invalidName) {

            assertThrows(IllegalArgumentException.class, () -> Workshop.rehydrate(10L, invalidName, true, true, List.of()));
        }
    }
}
