package fr.huiitre.tools.modules.dofus.sync.application.almanax;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import java.util.List;
import java.util.Optional;

import org.junit.jupiter.api.Test;

import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxRepository;
import fr.huiitre.tools.modules.dofus.almanax.domain.Almanax;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.domain.Item;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;

// Bonus du 23/09 (asset 8), texte réel de fr.json au 17/11/2026.
class SyncAlmanaxUseCaseTest {

    private static final GameVersionData DOFUS_3 = new GameVersionData(3L, "Dofus 3", "dofus3");
    private static final String RAW = "Butin augmenté sur les <b>{{monsterRace,71::Herboricoles}}</b>.";

    private final AlmanaxDataProvider provider = mock(AlmanaxDataProvider.class);
    private final AlmanaxRepository repository = mock(AlmanaxRepository.class);
    private final ItemRepository items = mock(ItemRepository.class);
    private final SyncAlmanaxUseCase useCase = new SyncAlmanaxUseCase(() -> 1L, provider, repository, items);

    private void given(String externalDescription) {
        Item item = mock(Item.class);
        when(item.getId()).thenReturn(42L);
        when(items.findByAssetId(anyLong(), anyLong())).thenReturn(Optional.of(item));
        when(repository.findAll()).thenReturn(List.of(
            Almanax.rehydrate(1L, 8L, "Butin", RAW, List.of("23/09/*"), 42L, 1L)));
        when(provider.fetchAll()).thenReturn(List.of(
            new AlmanaxSyncData(8L, "Butin", externalDescription, List.of("23/09/*"), 23564L, 1L)));
    }

    @Test
    void anUnchangedDescriptionWithATagIsNotReportedAsModified() {
        given(RAW);

        SyncReport report = useCase.execute(DOFUS_3);

        assertEquals(0, report.updatedCount());
        verify(repository, never()).update(any());
    }

    @Test
    void aRealDescriptionChangeIsStillReported() {
        given("Butin augmenté sur les <b>{{monsterRace,71::Herboricoles}}</b> et les <b>Kwaks</b>.");

        SyncReport report = useCase.execute(DOFUS_3);

        assertEquals(1, report.updatedCount());
        assertTrue(report.updated().get(0).contains("<b><b>Herboricoles</b></b> et les <b>Kwaks</b>"));
        verify(repository).update(any());
    }
}
