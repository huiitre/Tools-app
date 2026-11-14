package fr.huiitre.tools.modules.dofus.pricing.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.pricing.api.dto.ItemPricesBatchRequest;
import fr.huiitre.tools.modules.dofus.pricing.application.ports.CaptureAccountRepository;
import fr.huiitre.tools.modules.dofus.pricing.application.ports.ItemPriceRepository;

@Service
@Transactional
public class UpdateItemPricesUseCase implements SecuredUseCase {

    private final ItemPriceRepository itemPriceRepository;

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final CaptureAccountRepository captureAccountRepository;
    private final String captureUserEmail;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public UpdateItemPricesUseCase(
        ItemPriceRepository itemPriceRepository,
        AuthenticatedUserProvider authenticatedUserProvider,
        CaptureAccountRepository captureAccountRepository,
        @Value("${tools.dofus.pricing.capture-user-email}") String captureUserEmail
    ) {
        this.itemPriceRepository = itemPriceRepository; 
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.captureAccountRepository = captureAccountRepository;
        this.captureUserEmail = captureUserEmail;
    }

    public void execute(Long serverId, List<ItemPricesBatchRequest> requests, PriceSource source) {

        Long userId = source == PriceSource.CAPTURE
            ? captureAccountRepository.findUserIdByEmail(captureUserEmail)
                .orElseThrow(() -> new IllegalStateException("Compte de capture introuvable : " + captureUserEmail))
            : authenticatedUserProvider.getUserId();

        if (requests == null || requests.isEmpty()) {
            return;
        }

        for (ItemPricesBatchRequest request : requests) {
            itemPriceRepository.updateItemPrice(
                request.getItemId(),
                serverId,
                userId,
                request.getPrice()
            );
        }
    }
}