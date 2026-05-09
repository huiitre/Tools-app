package fr.huiitre.tools.modules.riot.valorant.api;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.valorant.application.command.AddToWatchlistCommand;
import fr.huiitre.tools.modules.riot.valorant.application.usecase.AddSkinToWatchlistUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.usecase.GetMyValorantWatchlistUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.usecase.RemoveSkinFromWatchlistUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantWatchlistEntryView;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/riot/valorant/watchlist")
public class ValorantWatchlistController {

    private final GetMyValorantWatchlistUseCase getMyValorantWatchlistUseCase;
    private final AddSkinToWatchlistUseCase addSkinToWatchlistUseCase;
    private final RemoveSkinFromWatchlistUseCase removeSkinFromWatchlistUseCase;

    public ValorantWatchlistController(
            GetMyValorantWatchlistUseCase getMyValorantWatchlistUseCase,
            AddSkinToWatchlistUseCase addSkinToWatchlistUseCase,
            RemoveSkinFromWatchlistUseCase removeSkinFromWatchlistUseCase) {
        this.getMyValorantWatchlistUseCase = getMyValorantWatchlistUseCase;
        this.addSkinToWatchlistUseCase = addSkinToWatchlistUseCase;
        this.removeSkinFromWatchlistUseCase = removeSkinFromWatchlistUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping
    public List<ValorantWatchlistEntryView> getMyWatchlist() {
        return getMyValorantWatchlistUseCase.execute();
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public ValorantWatchlistEntryView addToWatchlist(@RequestBody AddToWatchlistCommand command) {
        return addSkinToWatchlistUseCase.execute(command);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{skinId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void removeFromWatchlist(@PathVariable Long skinId) {
        removeSkinFromWatchlistUseCase.execute(skinId);
    }
}
