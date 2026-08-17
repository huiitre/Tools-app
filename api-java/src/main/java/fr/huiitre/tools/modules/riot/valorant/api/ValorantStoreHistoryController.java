package fr.huiitre.tools.modules.riot.valorant.api;

import fr.huiitre.tools.modules.riot.valorant.application.user.command.AddSkinToStoreHistoryCommand;
import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.AddSkinToStoreHistoryUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.GetMyValorantStoreHistoryUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantStoreHistoryView;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/riot/valorant/store-history")
public class ValorantStoreHistoryController {

    private final GetMyValorantStoreHistoryUseCase getMyValorantStoreHistoryUseCase;
    private final AddSkinToStoreHistoryUseCase addSkinToStoreHistoryUseCase;

    public ValorantStoreHistoryController(
            GetMyValorantStoreHistoryUseCase getMyValorantStoreHistoryUseCase,
            AddSkinToStoreHistoryUseCase addSkinToStoreHistoryUseCase) {
        this.getMyValorantStoreHistoryUseCase = getMyValorantStoreHistoryUseCase;
        this.addSkinToStoreHistoryUseCase = addSkinToStoreHistoryUseCase;
    }

    @GetMapping
    public List<ValorantStoreHistoryView> getMyStoreHistory(@RequestParam Long accountId) {
        return getMyValorantStoreHistoryUseCase.execute(accountId);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public void addToStoreHistory(@RequestBody AddSkinToStoreHistoryCommand command) {
        addSkinToStoreHistoryUseCase.execute(command);
    }
}
