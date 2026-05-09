package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantStoreHistoryRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantStoreHistoryView;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;

@Service
public class GetMyValorantStoreHistoryUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantStoreHistoryRepository storeHistoryRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetMyValorantStoreHistoryUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantStoreHistoryRepository storeHistoryRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.storeHistoryRepository = storeHistoryRepository;
    }

    public List<ValorantStoreHistoryView> execute() {
        Long userId = authenticatedUserProvider.getUserId();
        return storeHistoryRepository.findAllByUserId(userId);
    }
}
