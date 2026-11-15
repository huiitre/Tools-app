package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import java.util.List;
import java.util.Optional;

import org.junit.jupiter.api.Test;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.CreateWorkshopRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.CreateWorkshopTagRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.UpdateWorkshopRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.UpdateWorkshopTagRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopTagRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag.CreateWorkshopTagUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag.UpdateWorkshopTagUseCase;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

//* Un nom d'atelier ou de tag est unique par utilisateur et par version du jeu, pas tous jeux confondus.
class WorkshopNameUniquenessTest {

    private static final long USER = 1L;
    private static final long RETRO = 1L;
    private static final long DOFUS_3 = 3L;

    private final AuthenticatedUserProvider user = () -> USER;
    private final WorkshopRepository workshops = mock(WorkshopRepository.class);
    private final WorkshopTagRepository tags = mock(WorkshopTagRepository.class);

    @Test
    void workshopCreationChecksTheNameInTheRequestedGameVersionOnly() {
        when(workshops.existsByUserIdAndGameVersionIdAndName(USER, RETRO, "Bijoutier 100")).thenReturn(true);
        when(workshops.create(anyLong(), anyLong(), any())).thenReturn(42L);

        assertEquals(42L, new CreateWorkshopUseCase(user, workshops)
            .execute(DOFUS_3, new CreateWorkshopRequest("Bijoutier 100")).getId());
        verify(workshops).existsByUserIdAndGameVersionIdAndName(USER, DOFUS_3, "Bijoutier 100");
    }

    @Test
    void workshopCreationIsRefusedWhenTheNameExistsInTheSameGameVersion() {
        when(workshops.existsByUserIdAndGameVersionIdAndName(USER, RETRO, "Bijoutier 100")).thenReturn(true);

        CreateWorkshopUseCase useCase = new CreateWorkshopUseCase(user, workshops);
        assertThrows(IllegalArgumentException.class,
            () -> useCase.execute(RETRO, new CreateWorkshopRequest("Bijoutier 100")));
        verify(workshops, never()).create(anyLong(), anyLong(), any());
    }

    @Test
    void workshopRenameChecksAgainstTheOtherWorkshopsOfItsOwnGameVersion() {
        when(workshops.findByIdAndUserId(USER, 8L))
            .thenReturn(Optional.of(Workshop.rehydrate(8L, "Trophées", true, false, List.of())));
        when(workshops.findAllTagsByUserIdAndWorkshopId(USER, 8L)).thenReturn(List.of());

        new UpdateWorkshopUseCase(user, workshops).execute(8L, new UpdateWorkshopRequest("Bijoutier 100", null, null));

        verify(workshops).existsOtherWithNameInSameGameVersion(USER, 8L, "Bijoutier 100");
        verify(workshops).update(anyLong(), any());
    }

    @Test
    void workshopRenameIsRefusedWhenAnotherWorkshopOfTheSameVersionHasTheName() {
        when(workshops.findByIdAndUserId(USER, 8L))
            .thenReturn(Optional.of(Workshop.rehydrate(8L, "Trophées", true, false, List.of())));
        when(workshops.existsOtherWithNameInSameGameVersion(USER, 8L, "Bijoutier 100")).thenReturn(true);

        UpdateWorkshopUseCase useCase = new UpdateWorkshopUseCase(user, workshops);
        assertThrows(IllegalArgumentException.class,
            () -> useCase.execute(8L, new UpdateWorkshopRequest("Bijoutier 100", null, null)));
        verify(workshops, never()).update(anyLong(), any());
    }

    @Test
    void tagCreationChecksTheNameInTheRequestedGameVersionOnly() {
        when(tags.existsByUserIdAndGameVersionIdAndName(USER, RETRO, "Métiers")).thenReturn(true);
        when(tags.create(anyLong(), anyLong(), any())).thenReturn(17L);

        assertEquals(17L, new CreateWorkshopTagUseCase(user, tags)
            .execute(DOFUS_3, new CreateWorkshopTagRequest("Métiers", "#ff0000")).getId());
        verify(tags).existsByUserIdAndGameVersionIdAndName(USER, DOFUS_3, "Métiers");
    }

    @Test
    void tagCreationIsRefusedWhenTheNameExistsInTheSameGameVersion() {
        when(tags.existsByUserIdAndGameVersionIdAndName(USER, RETRO, "Métiers")).thenReturn(true);

        CreateWorkshopTagUseCase useCase = new CreateWorkshopTagUseCase(user, tags);
        assertThrows(IllegalArgumentException.class,
            () -> useCase.execute(RETRO, new CreateWorkshopTagRequest("Métiers", "#ff0000")));
        verify(tags, never()).create(anyLong(), anyLong(), any());
    }

    @Test
    void tagRenameChecksAgainstTheOtherTagsOfItsOwnGameVersion() {
        when(tags.findByIdAndUserId(USER, 8L)).thenReturn(Optional.of(WorkshopTag.rehydrate(8L, "Cra retro", "#00ff00")));

        new UpdateWorkshopTagUseCase(user, tags).execute(8L, new UpdateWorkshopTagRequest("Cra", "#00ff00"));

        verify(tags).existsOtherWithNameInSameGameVersion(USER, 8L, "Cra");
        verify(tags).update(anyLong(), any());
    }

    @Test
    void tagRenameIsRefusedWhenAnotherTagOfTheSameVersionHasTheName() {
        when(tags.findByIdAndUserId(USER, 8L)).thenReturn(Optional.of(WorkshopTag.rehydrate(8L, "Cra retro", "#00ff00")));
        when(tags.existsOtherWithNameInSameGameVersion(USER, 8L, "Enu retro")).thenReturn(true);

        UpdateWorkshopTagUseCase useCase = new UpdateWorkshopTagUseCase(user, tags);
        assertThrows(IllegalArgumentException.class,
            () -> useCase.execute(8L, new UpdateWorkshopTagRequest("Enu retro", "#00ff00")));
        verify(tags, never()).update(anyLong(), any());
        verify(tags, never()).existsByUserIdAndGameVersionIdAndName(anyLong(), anyLong(), anyString());
    }
}
