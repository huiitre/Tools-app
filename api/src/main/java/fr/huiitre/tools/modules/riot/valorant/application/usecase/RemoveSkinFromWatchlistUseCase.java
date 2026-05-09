package fr.huiitre.tools.modules.riot.valorant.application.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantWatchlistRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Optional;

@Service
@Transactional
public class RemoveSkinFromWatchlistUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantWatchlistRepository watchlistRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public RemoveSkinFromWatchlistUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantWatchlistRepository watchlistRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.watchlistRepository = watchlistRepository;
    }

    public void execute(Long skinId) {
        Long userId = authenticatedUserProvider.getUserId();
        watchlistRepository.remove(userId, skinId);
    }
}
