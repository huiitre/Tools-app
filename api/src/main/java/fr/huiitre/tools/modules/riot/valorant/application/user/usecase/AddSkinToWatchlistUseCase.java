package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
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
    private final ValorantAuthRepository valorantAuthRepository;
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
            ValorantAuthRepository valorantAuthRepository,
            ValorantSkinRepository skinRepository,
            ValorantWatchlistRepository watchlistRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.valorantAuthRepository = valorantAuthRepository;
        this.skinRepository = skinRepository;
        this.watchlistRepository = watchlistRepository;
    }

    public ValorantSkinView execute(AddToWatchlistCommand command) {
        Long accountId = command.getAccountId();
        if (!valorantAuthRepository.existsByIdAndUserId(accountId, authenticatedUserProvider.getUserId())) {
            throw new IllegalArgumentException("VALORANT_ACCOUNT_NOT_FOUND");
        }
        if (watchlistRepository.existsByAccountIdAndSkinId(accountId, command.getSkinId())) {
            throw new IllegalArgumentException("SKIN_ALREADY_IN_WATCHLIST");
        }

        watchlistRepository.add(accountId, command.getSkinId());

        return skinRepository.findById(command.getSkinId(), accountId)
                .orElseThrow(() -> new IllegalArgumentException("SKIN_NOT_FOUND"));
    }
}
