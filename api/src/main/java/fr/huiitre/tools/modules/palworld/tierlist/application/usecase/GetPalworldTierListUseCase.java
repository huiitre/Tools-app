package fr.huiitre.tools.modules.palworld.tierlist.application.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.PalworldTierListProvider;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.PalworldTierGroupView;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Map;
import java.util.Optional;

@Service
public class GetPalworldTierListUseCase implements SecuredUseCase {

    private final PalworldTierListProvider tierListProvider;

    public GetPalworldTierListUseCase(PalworldTierListProvider tierListProvider) {
        this.tierListProvider = tierListProvider;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public Map<String, List<PalworldTierGroupView>> execute() {
        return tierListProvider.getTierLists();
    }
}
