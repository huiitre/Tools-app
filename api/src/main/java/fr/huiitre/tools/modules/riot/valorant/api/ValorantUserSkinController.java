package fr.huiitre.tools.modules.riot.valorant.api;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.user.command.AddUserSkinCommand;
import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.AddMyValorantSkinUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.GetMyValorantUserSkinsUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.RemoveMyValorantSkinUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/riot/valorant/my-skins")
public class ValorantUserSkinController {

    private final GetMyValorantUserSkinsUseCase getMyValorantUserSkinsUseCase;
    private final AddMyValorantSkinUseCase addMyValorantSkinUseCase;
    private final RemoveMyValorantSkinUseCase removeMyValorantSkinUseCase;

    public ValorantUserSkinController(
            GetMyValorantUserSkinsUseCase getMyValorantUserSkinsUseCase,
            AddMyValorantSkinUseCase addMyValorantSkinUseCase,
            RemoveMyValorantSkinUseCase removeMyValorantSkinUseCase) {
        this.getMyValorantUserSkinsUseCase = getMyValorantUserSkinsUseCase;
        this.addMyValorantSkinUseCase = addMyValorantSkinUseCase;
        this.removeMyValorantSkinUseCase = removeMyValorantSkinUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping
    public List<ValorantSkinView> getMyUserSkins() {
        return getMyValorantUserSkinsUseCase.execute();
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public ValorantSkinView addMySkin(@RequestBody AddUserSkinCommand command) {
        return addMyValorantSkinUseCase.execute(command);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{skinId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void removeMySkin(@PathVariable Long skinId) {
        removeMyValorantSkinUseCase.execute(skinId);
    }
}
