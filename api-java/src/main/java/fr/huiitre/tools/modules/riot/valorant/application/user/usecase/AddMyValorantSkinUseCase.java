package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.command.AddUserSkinCommand;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantUserSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Optional;

@Service
@Transactional
public class AddMyValorantSkinUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantAuthRepository valorantAuthRepository;
    private final ValorantSkinRepository skinRepository;
    private final ValorantUserSkinRepository userSkinRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public AddMyValorantSkinUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantAuthRepository valorantAuthRepository,
            ValorantSkinRepository skinRepository,
            ValorantUserSkinRepository userSkinRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.valorantAuthRepository = valorantAuthRepository;
        this.skinRepository = skinRepository;
        this.userSkinRepository = userSkinRepository;
    }

    public ValorantSkinView execute(AddUserSkinCommand command) {
        Long userId = authenticatedUserProvider.getUserId();
        Long accountId = command.getAccountId();

        if (!valorantAuthRepository.existsByIdAndUserId(accountId, userId)) {
            throw new IllegalArgumentException("VALORANT_ACCOUNT_NOT_FOUND");
        }
        if (userSkinRepository.existsByAccountIdAndSkinId(accountId, command.getSkinId())) {
            throw new IllegalArgumentException("SKIN_ALREADY_OWNED");
        }

        userSkinRepository.add(accountId, command.getSkinId());

        return skinRepository.findById(command.getSkinId(), accountId)
                .orElseThrow(() -> new IllegalArgumentException("SKIN_NOT_FOUND"));
    }
}
