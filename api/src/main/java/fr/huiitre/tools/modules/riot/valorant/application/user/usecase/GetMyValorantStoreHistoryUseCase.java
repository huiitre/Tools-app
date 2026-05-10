package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantStoreHistoryRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantStoreHistoryView;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;

@Service
public class GetMyValorantStoreHistoryUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantStoreHistoryRepository storeHistoryRepository;
    private final ValorantSkinRepository skinRepository;

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
            ValorantStoreHistoryRepository storeHistoryRepository,
            ValorantSkinRepository skinRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.storeHistoryRepository = storeHistoryRepository;
        this.skinRepository = skinRepository;
    }

    public List<ValorantStoreHistoryView> execute() {
        Long userId = authenticatedUserProvider.getUserId();
        return storeHistoryRepository.findAllRawByUserId(userId).entrySet().stream()
                .map(entry -> new ValorantStoreHistoryView(
                        entry.getKey(),
                        entry.getValue().stream()
                                .map(skinId -> skinRepository.findById(skinId, userId))
                                .filter(Optional::isPresent)
                                .map(Optional::get)
                                .collect(Collectors.toList())
                ))
                .collect(Collectors.toList());
    }
}
