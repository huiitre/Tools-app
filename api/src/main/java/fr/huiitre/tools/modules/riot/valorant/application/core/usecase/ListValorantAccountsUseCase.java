package fr.huiitre.tools.modules.riot.valorant.application.core.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantAccountView;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;

@Service
public class ListValorantAccountsUseCase implements SecuredUseCase {

    private final ValorantAuthRepository valorantAuthRepository;
    private final AuthenticatedUserProvider authenticatedUserProvider;

    public ListValorantAccountsUseCase(ValorantAuthRepository valorantAuthRepository,
                                        AuthenticatedUserProvider authenticatedUserProvider) {
        this.valorantAuthRepository = valorantAuthRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public List<ValorantAccountView> execute() {
        Long userId = authenticatedUserProvider.getUserId();
        return valorantAuthRepository.findAllByUserId(userId).stream()
                .map(a -> new ValorantAccountView(a.id(), a.puuid(), a.region(), a.gameName(), a.tagLine(), a.label()))
                .toList();
    }
}
