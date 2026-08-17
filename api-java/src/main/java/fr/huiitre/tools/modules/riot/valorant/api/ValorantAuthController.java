package fr.huiitre.tools.modules.riot.valorant.api;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.core.command.LinkValorantAccountCommand;
import fr.huiitre.tools.modules.riot.valorant.application.core.command.RenameValorantAccountCommand;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.GetValorantAccessTokenUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.LinkValorantAccountUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.ListValorantAccountsUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.RenameValorantAccountUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.usecase.UnlinkValorantAccountUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantAccountAuthView;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantAccountView;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantTokenView;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/riot/valorant/accounts")
public class ValorantAuthController {

    private final LinkValorantAccountUseCase linkValorantAccountUseCase;
    private final ListValorantAccountsUseCase listValorantAccountsUseCase;
    private final UnlinkValorantAccountUseCase unlinkValorantAccountUseCase;
    private final RenameValorantAccountUseCase renameValorantAccountUseCase;
    private final GetValorantAccessTokenUseCase getValorantAccessTokenUseCase;

    public ValorantAuthController(LinkValorantAccountUseCase linkValorantAccountUseCase,
                                   ListValorantAccountsUseCase listValorantAccountsUseCase,
                                   UnlinkValorantAccountUseCase unlinkValorantAccountUseCase,
                                   RenameValorantAccountUseCase renameValorantAccountUseCase,
                                   GetValorantAccessTokenUseCase getValorantAccessTokenUseCase) {
        this.linkValorantAccountUseCase = linkValorantAccountUseCase;
        this.listValorantAccountsUseCase = listValorantAccountsUseCase;
        this.unlinkValorantAccountUseCase = unlinkValorantAccountUseCase;
        this.renameValorantAccountUseCase = renameValorantAccountUseCase;
        this.getValorantAccessTokenUseCase = getValorantAccessTokenUseCase;
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public ValorantAccountAuthView linkAccount(@RequestBody LinkValorantAccountCommand command) {
        return linkValorantAccountUseCase.execute(command);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping
    public List<ValorantAccountView> listAccounts() {
        return listValorantAccountsUseCase.execute();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/{accountId}/refresh")
    public ValorantTokenView refresh(@PathVariable Long accountId) {
        return getValorantAccessTokenUseCase.execute(accountId);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{accountId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void unlinkAccount(@PathVariable Long accountId) {
        unlinkValorantAccountUseCase.execute(accountId);
    }

    @RequiredRole(RoleCode.USER)
    @PutMapping("/{accountId}")
    public ValorantAccountView renameAccount(@PathVariable Long accountId, @RequestBody RenameValorantAccountCommand command) {
        return renameValorantAccountUseCase.execute(accountId, command);
    }
}
