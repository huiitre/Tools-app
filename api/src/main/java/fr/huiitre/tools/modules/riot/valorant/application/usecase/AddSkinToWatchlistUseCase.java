package fr.huiitre.tools.modules.riot.valorant.application.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.command.AddToWatchlistCommand;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantWatchlistRepository;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantSkinView;
import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantWatchlistEntryView;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
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

    public ValorantWatchlistEntryView execute(AddToWatchlistCommand command) {
        Long userId = authenticatedUserProvider.getUserId();

        ValorantSkinView skin = skinRepository.findById(command.getSkinId())
                .orElseThrow(() -> new IllegalArgumentException("SKIN_NOT_FOUND"));

        if (watchlistRepository.existsByUserIdAndSkinId(userId, command.getSkinId())) {
            throw new IllegalArgumentException("SKIN_ALREADY_IN_WATCHLIST");
        }

        Long watchlistId = watchlistRepository.add(userId, command.getSkinId());
        return new ValorantWatchlistEntryView(watchlistId, skin.id(), skin.name(), skin.iconUrl(), LocalDateTime.now());
    }
}
