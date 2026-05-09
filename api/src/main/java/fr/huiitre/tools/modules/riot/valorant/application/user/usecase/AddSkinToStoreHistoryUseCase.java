package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.command.AddSkinToStoreHistoryCommand;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantStoreHistoryRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantSkinView;
import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantStoreHistoryView;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.util.Optional;

@Service
@Transactional
public class AddSkinToStoreHistoryUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantSkinRepository skinRepository;
    private final ValorantStoreHistoryRepository storeHistoryRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public AddSkinToStoreHistoryUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantSkinRepository skinRepository,
            ValorantStoreHistoryRepository storeHistoryRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.skinRepository = skinRepository;
        this.storeHistoryRepository = storeHistoryRepository;
    }

    public ValorantStoreHistoryView execute(AddSkinToStoreHistoryCommand command) {
        Long userId = authenticatedUserProvider.getUserId();

        ValorantSkinView skin = skinRepository.findById(command.getSkinId())
                .orElseThrow(() -> new IllegalArgumentException("SKIN_NOT_FOUND"));

        if (storeHistoryRepository.existsByUserIdAndSkinIdAndDate(userId, command.getSkinId())) {
            throw new IllegalArgumentException("SKIN_ALREADY_IN_STORE_HISTORY_FOR_TODAY");
        }

        Long historyId = storeHistoryRepository.add(userId, command.getSkinId());
        return new ValorantStoreHistoryView(historyId, skin.id(), skin.name(), skin.iconUrl(), LocalDate.now());
    }
}
