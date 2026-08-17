package fr.huiitre.tools.modules.palworld.tierlist.api;

import java.util.List;
import java.util.Map;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.tierlist.application.TierListSyncReport;
import fr.huiitre.tools.modules.palworld.tierlist.application.usecase.GetTierListUseCase;
import fr.huiitre.tools.modules.palworld.tierlist.application.usecase.SyncTierListUseCase;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.TierGroupView;

@RestController
@RequestMapping("/palworld/tierlist")
public class PalworldTierListController {

    private final GetTierListUseCase getTierListUseCase;
    private final SyncTierListUseCase syncTierListUseCase;

    public PalworldTierListController(GetTierListUseCase getTierListUseCase, SyncTierListUseCase syncTierListUseCase) {
        this.getTierListUseCase = getTierListUseCase;
        this.syncTierListUseCase = syncTierListUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping
    public Map<String, Map<String, List<TierGroupView>>> getTierLists() {
        return getTierListUseCase.execute();
    }

    @RequiredRole(RoleCode.TECH)
    @PostMapping("/admin/sync")
    public TierListSyncReport sync() {
        return syncTierListUseCase.execute();
    }
}
