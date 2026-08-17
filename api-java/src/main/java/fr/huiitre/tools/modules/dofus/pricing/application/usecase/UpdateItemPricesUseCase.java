package fr.huiitre.tools.modules.dofus.pricing.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.pricing.api.dto.ItemPricesBatchRequest;
import fr.huiitre.tools.modules.dofus.pricing.application.ports.ItemPriceRepository;

@Service
@Transactional
public class UpdateItemPricesUseCase implements SecuredUseCase {

    private final ItemPriceRepository itemPriceRepository;

    private final AuthenticatedUserProvider authenticatedUserProvider;

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
        AuthenticatedUserProvider authenticatedUserProvider
    ) {
        this.itemPriceRepository = itemPriceRepository; 
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public void execute(Long serverId, List<ItemPricesBatchRequest> requests) {

        Long userId = authenticatedUserProvider.getUserId();

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