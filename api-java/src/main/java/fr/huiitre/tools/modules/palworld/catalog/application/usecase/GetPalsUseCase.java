package fr.huiitre.tools.modules.palworld.catalog.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.catalog.application.ports.PalCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.view.PalListItemView;

@Service
public class GetPalsUseCase implements SecuredUseCase {

    private final PalCatalogRepository palCatalogRepository;

    public GetPalsUseCase(PalCatalogRepository palCatalogRepository) {
        this.palCatalogRepository = palCatalogRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public List<PalListItemView> execute() {
        return palCatalogRepository.findAll();
    }
}
