package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.command.AddToWatchlistCommand;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantWatchlistRepository;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Optional;

@Service
@Transactional
public class AddSkinToWatchlistUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantSkinRepository skinRepository;
    private final ValorantWatchlistRepository watchlistRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public AddSkinToWatchlistUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantSkinRepository skinRepository,
            ValorantWatchlistRepository watchlistRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.skinRepository = skinRepository;
        this.watchlistRepository = watchlistRepository;
    }

    public ValorantSkinView execute(AddToWatchlistCommand command) {
        Long userId = authenticatedUserProvider.getUserId();

        if (watchlistRepository.existsByUserIdAndSkinId(userId, command.getSkinId())) {
            throw new IllegalArgumentException("SKIN_ALREADY_IN_WATCHLIST");
        }

        watchlistRepository.add(userId, command.getSkinId());
        
        return skinRepository.findById(command.getSkinId(), userId)
                .orElseThrow(() -> new IllegalArgumentException("SKIN_NOT_FOUND"));
    }
}
