package fr.huiitre.tools.modules.palworld.tierlist.api;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.tierlist.application.usecase.GetPalworldTierListUseCase;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.PalworldTierGroupView;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/palworld/tierlist")
public class PalworldTierListController {

    private final GetPalworldTierListUseCase getPalworldTierListUseCase;

    public PalworldTierListController(GetPalworldTierListUseCase getPalworldTierListUseCase) {
        this.getPalworldTierListUseCase = getPalworldTierListUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping
    public Map<String, List<PalworldTierGroupView>> getTierLists() {
        return getPalworldTierListUseCase.execute();
    }
}
