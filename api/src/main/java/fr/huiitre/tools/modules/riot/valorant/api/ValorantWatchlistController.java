package fr.huiitre.tools.modules.riot.valorant.api;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.user.command.AddToWatchlistCommand;
import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.AddSkinToWatchlistUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.GetMyValorantWatchlistUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.RemoveSkinFromWatchlistUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.ValorantWatchlistNotifier;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/riot/valorant/watchlist")
public class ValorantWatchlistController {

    private final GetMyValorantWatchlistUseCase getMyWatchlistUseCase;
    private final AddSkinToWatchlistUseCase addSkinToWatchlistUseCase;
    private final RemoveSkinFromWatchlistUseCase removeSkinFromWatchlistUseCase;
    private final ValorantWatchlistNotifier watchlistNotifier;

    public ValorantWatchlistController(
            GetMyValorantWatchlistUseCase getMyWatchlistUseCase,
            AddSkinToWatchlistUseCase addSkinToWatchlistUseCase,
            RemoveSkinFromWatchlistUseCase removeSkinFromWatchlistUseCase,
            ValorantWatchlistNotifier watchlistNotifier) {
        this.getMyWatchlistUseCase = getMyWatchlistUseCase;
        this.addSkinToWatchlistUseCase = addSkinToWatchlistUseCase;
        this.removeSkinFromWatchlistUseCase = removeSkinFromWatchlistUseCase;
        this.watchlistNotifier = watchlistNotifier;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping
    public List<ValorantSkinView> getMyWatchlist() {
        return getMyWatchlistUseCase.execute();
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public ValorantSkinView addToWatchlist(@RequestBody AddToWatchlistCommand command) {
        return addSkinToWatchlistUseCase.execute(command);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{skinId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void removeFromWatchlist(@PathVariable Long skinId) {
        removeSkinFromWatchlistUseCase.execute(skinId);
    }

    @RequiredRole(RoleCode.ADMIN)
    @PostMapping("/admin/sync")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void triggerSync() {
        watchlistNotifier.processAllUsers();
    }
}
