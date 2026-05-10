package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.riot.valorant.application.user.command.AddSkinToStoreHistoryCommand;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantStoreHistoryRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.LocalDate;
import java.util.List;

import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class AddSkinToStoreHistoryUseCaseTest {

    @Mock
    private AuthenticatedUserProvider authenticatedUserProvider;

    @Mock
    private ValorantStoreHistoryRepository storeHistoryRepository;

    @InjectMocks
    private AddSkinToStoreHistoryUseCase useCase;

    private final Long userId = 42L;

    @BeforeEach
    void setUp() {
        lenient().when(authenticatedUserProvider.getUserId()).thenReturn(userId);
    }

    @Test
    void shouldAddMultipleSkinsToHistory() {
        // Given
        List<Long> skinIds = List.of(101L, 102L);
        LocalDate date = LocalDate.of(2026, 5, 10);
        AddSkinToStoreHistoryCommand command = new AddSkinToStoreHistoryCommand(skinIds, date);

        when(storeHistoryRepository.existsByUserIdAndSkinIdAndDate(eq(userId), anyLong(), eq(date))).thenReturn(false);

        // When
        useCase.execute(command);

        // Then
        verify(storeHistoryRepository).add(userId, 101L, date);
        verify(storeHistoryRepository).add(userId, 102L, date);
    }

    @Test
    void shouldNotAddDuplicateSkinsOnSameDay() {
        // Given
        List<Long> skinIds = List.of(101L);
        LocalDate date = LocalDate.of(2026, 5, 10);
        AddSkinToStoreHistoryCommand command = new AddSkinToStoreHistoryCommand(skinIds, date);

        when(storeHistoryRepository.existsByUserIdAndSkinIdAndDate(userId, 101L, date)).thenReturn(true);

        // When
        useCase.execute(command);

        // Then
        verify(storeHistoryRepository, never()).add(anyLong(), anyLong(), any());
    }
}
