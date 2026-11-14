package fr.huiitre.tools.modules.dofus.pricing.application.usecase;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import org.junit.jupiter.api.Test;

import fr.huiitre.tools.modules.dofus.pricing.api.dto.ItemPricesBatchRequest;
import fr.huiitre.tools.modules.dofus.pricing.application.ports.ItemPriceRepository;
import fr.huiitre.tools.modules.dofus.pricing.application.view.ItemPriceDto;

class UpdateItemPricesUseCaseTest {

    private static final long CONNECTED_USER = 1L;
    private static final long CAPTURE_USER = 5L;
    private static final String CAPTURE_EMAIL = "capture@example.org";

    private final List<long[]> writes = new ArrayList<>();

    private final ItemPriceRepository repository = new ItemPriceRepository() {
        @Override
        public List<ItemPriceDto> findPricesByItemIds(List<Long> itemIds, Long userId, Long serverId) {
            return List.of();
        }

        @Override
        public void updateItemPrice(Long itemId, Long serverId, Long userId, Long price) {
            writes.add(new long[] { itemId, serverId, userId, price });
        }
    };

    private UpdateItemPricesUseCase useCase(Optional<Long> captureAccount) {
        return new UpdateItemPricesUseCase(
            repository,
            () -> CONNECTED_USER,
            email -> CAPTURE_EMAIL.equals(email) ? captureAccount : Optional.empty(),
            CAPTURE_EMAIL);
    }

    private static List<ItemPricesBatchRequest> prices() {
        return List.of(new ItemPricesBatchRequest(42L, 1000L), new ItemPricesBatchRequest(43L, 2000L));
    }

    @Test
    void manualPricesAreSavedForTheConnectedUser() {
        useCase(Optional.of(CAPTURE_USER)).execute(1L, prices(), PriceSource.MANUAL);
        assertEquals(List.of(CONNECTED_USER, CONNECTED_USER), writes.stream().map(w -> w[2]).toList());
    }

    @Test
    void capturedPricesAreSavedForTheCaptureAccount() {
        useCase(Optional.of(CAPTURE_USER)).execute(1L, prices(), PriceSource.CAPTURE);
        assertEquals(List.of(CAPTURE_USER, CAPTURE_USER), writes.stream().map(w -> w[2]).toList());
        assertEquals(List.of(1000L, 2000L), writes.stream().map(w -> w[3]).toList());
    }

    @Test
    void missingCaptureAccountFailsInsteadOfFallingBackToTheUser() {
        UpdateItemPricesUseCase useCase = useCase(Optional.empty());
        assertThrows(IllegalStateException.class, () -> useCase.execute(1L, prices(), PriceSource.CAPTURE));
        assertEquals(0, writes.size());
    }

    @Test
    void sourceParameterIsParsed() {
        assertEquals(PriceSource.MANUAL, PriceSource.fromParam(null));
        assertEquals(PriceSource.MANUAL, PriceSource.fromParam(""));
        assertEquals(PriceSource.CAPTURE, PriceSource.fromParam("capture"));
        assertEquals(PriceSource.CAPTURE, PriceSource.fromParam(" CAPTURE "));
        assertThrows(IllegalArgumentException.class, () -> PriceSource.fromParam("robot"));
    }
}
