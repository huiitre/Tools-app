package fr.huiitre.tools.modules.dofus.catalogue.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.catalogue.application.data.CatalogueColumnsDefinition;
import fr.huiitre.tools.modules.dofus.catalogue.application.dto.CatalogueColumnDto;

@Service
@Transactional
public class GetCatalogueColumnsUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.empty();
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetCatalogueColumnsUseCase(
        AuthenticatedUserProvider authenticatedUserProvider
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public List<CatalogueColumnDto> execute() {
        return CatalogueColumnsDefinition.all();
    }
}