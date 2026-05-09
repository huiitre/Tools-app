package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.user.command.AddUserSkinCommand;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantUserSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantSkinView;
import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantUserSkinView;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.Optional;

@Service
@Transactional
public class AddMyValorantSkinUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
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
            ValorantSkinRepository skinRepository,
            ValorantUserSkinRepository userSkinRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.skinRepository = skinRepository;
        this.userSkinRepository = userSkinRepository;
    }

    public ValorantUserSkinView execute(AddUserSkinCommand command) {
        Long userId = authenticatedUserProvider.getUserId();

        ValorantSkinView skin = skinRepository.findById(command.getSkinId())
                .orElseThrow(() -> new IllegalArgumentException("SKIN_NOT_FOUND"));

        if (userSkinRepository.existsByUserIdAndSkinId(userId, command.getSkinId())) {
            throw new IllegalArgumentException("SKIN_ALREADY_OWNED");
        }

        Long userSkinId = userSkinRepository.add(userId, command.getSkinId());
        return new ValorantUserSkinView(userSkinId, skin.id(), skin.name(), skin.iconUrl(), LocalDateTime.now());
    }
}
