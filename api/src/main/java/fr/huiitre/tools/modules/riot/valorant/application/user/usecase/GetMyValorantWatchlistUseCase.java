package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantWatchlistRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantWatchlistEntryView;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;

@Service
public class GetMyValorantWatchlistUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantWatchlistRepository watchlistRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetMyValorantWatchlistUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantWatchlistRepository watchlistRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.watchlistRepository = watchlistRepository;
    }

    public List<ValorantWatchlistEntryView> execute() {
        Long userId = authenticatedUserProvider.getUserId();
        return watchlistRepository.findAllByUserId(userId);
    }
}
