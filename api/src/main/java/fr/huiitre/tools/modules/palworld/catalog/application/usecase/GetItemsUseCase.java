package fr.huiitre.tools.modules.palworld.catalog.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.catalog.application.ports.ItemCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.view.ItemCatalogView;

@Service
public class GetItemsUseCase implements SecuredUseCase {

    private final ItemCatalogRepository itemCatalogRepository;

    public GetItemsUseCase(ItemCatalogRepository itemCatalogRepository) {
        this.itemCatalogRepository = itemCatalogRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public List<ItemCatalogView> execute() {
        return itemCatalogRepository.findAll();
    }
}
