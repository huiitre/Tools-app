package fr.huiitre.tools.modules.riot.valorant.application.catalog.usecase;

import java.util.Optional;
import java.util.UUID;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

@Service
public class GetValorantSkinByAssetIdUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantAuthRepository valorantAuthRepository;
    private final ValorantSkinRepository skinRepository;

    public GetValorantSkinByAssetIdUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantAuthRepository valorantAuthRepository,
            ValorantSkinRepository skinRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.valorantAuthRepository = valorantAuthRepository;
        this.skinRepository = skinRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ValorantSkinView execute(UUID assetId, Long accountId) {
        if (accountId != null && !valorantAuthRepository.existsByIdAndUserId(accountId, authenticatedUserProvider.getUserId())) {
            throw new IllegalArgumentException("VALORANT_ACCOUNT_NOT_FOUND");
        }
        return skinRepository.findByAssetId(assetId, accountId)
                .orElseThrow(() -> new IllegalArgumentException("Skin not found: " + assetId));
    }
}
