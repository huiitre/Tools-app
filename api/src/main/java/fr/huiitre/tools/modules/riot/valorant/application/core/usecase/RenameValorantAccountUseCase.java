package fr.huiitre.tools.modules.riot.valorant.application.core.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.command.RenameValorantAccountCommand;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantAccountView;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class RenameValorantAccountUseCase implements SecuredUseCase {

    private final ValorantAuthRepository valorantAuthRepository;
    private final AuthenticatedUserProvider authenticatedUserProvider;

    public RenameValorantAccountUseCase(ValorantAuthRepository valorantAuthRepository,
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
        return RoleCode.USER;
    }

    public ValorantAccountView execute(Long accountId, RenameValorantAccountCommand command) {
        Long userId = authenticatedUserProvider.getUserId();
        if (!valorantAuthRepository.existsByIdAndUserId(accountId, userId)) {
            throw new IllegalArgumentException("VALORANT_ACCOUNT_NOT_FOUND");
        }
        if (command.label() == null || command.label().isBlank()) {
            throw new IllegalArgumentException("LABEL_REQUIRED");
        }

        valorantAuthRepository.updateLabel(accountId, command.label().trim());

        return valorantAuthRepository.findAllByUserId(userId).stream()
                .filter(a -> a.id() == accountId)
                .findFirst()
                .map(a -> new ValorantAccountView(a.id(), a.puuid(), a.region(), a.gameName(), a.tagLine(), a.label()))
                .orElseThrow(() -> new IllegalArgumentException("VALORANT_ACCOUNT_NOT_FOUND"));
    }
}
