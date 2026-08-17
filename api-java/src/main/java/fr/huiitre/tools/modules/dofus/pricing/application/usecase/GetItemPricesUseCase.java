package fr.huiitre.tools.modules.dofus.pricing.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.pricing.application.ports.ItemPriceRepository;
import fr.huiitre.tools.modules.dofus.pricing.application.view.ItemPriceDto;

@Service
@Transactional(readOnly = true)
public class GetItemPricesUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ItemPriceRepository itemPriceRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetItemPricesUseCase(
        ItemPriceRepository itemPriceRepository,
        AuthenticatedUserProvider authenticatedUserProvider
    ) {
        this.itemPriceRepository = itemPriceRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public List<ItemPriceDto> execute(Long serverId, List<Long> itemIds) {

        Long userId = authenticatedUserProvider.getUserId();

        if (itemIds.isEmpty()) {
            return List.of();
        }

        return itemPriceRepository.findPricesByItemIds(itemIds, userId, serverId);
    }
}