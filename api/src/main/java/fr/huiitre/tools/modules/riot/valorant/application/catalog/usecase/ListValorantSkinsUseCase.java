package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantSkinView;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;

@Service
public class ListValorantSkinsUseCase implements SecuredUseCase {

    private final ValorantSkinRepository skinRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ListValorantSkinsUseCase(ValorantSkinRepository skinRepository) {
        this.skinRepository = skinRepository;
    }

    public List<ValorantSkinView> execute() {
        return skinRepository.findAll();
    }
}
