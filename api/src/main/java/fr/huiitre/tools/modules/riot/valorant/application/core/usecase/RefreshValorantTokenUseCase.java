package fr.huiitre.tools.modules.riot.valorant.application.core.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.security.infrastructure.EncryptionService;
import fr.huiitre.tools.modules.riot.valorant.application.core.command.RefreshTokenCommand;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.RiotAuthPort;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantTokenView;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class RefreshValorantTokenUseCase implements SecuredUseCase {

    private final RiotAuthPort riotAuthPort;
    private final ValorantAuthRepository valorantAuthRepository;
    private final EncryptionService encryptionService;
    private final CurrentUserProvider currentUserProvider;

    public RefreshValorantTokenUseCase(RiotAuthPort riotAuthPort,
                                       ValorantAuthRepository valorantAuthRepository,
                                       EncryptionService encryptionService,
                                       CurrentUserProvider currentUserProvider) {
        this.riotAuthPort = riotAuthPort;
        this.valorantAuthRepository = valorantAuthRepository;
        this.encryptionService = encryptionService;
        this.currentUserProvider = currentUserProvider;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ValorantTokenView execute(RefreshTokenCommand command) {
        if (command.refreshToken() == null || command.refreshToken().isBlank()) {
            throw new IllegalArgumentException("REFRESH_TOKEN_REQUIRED");
        }
        if (command.region() == null || command.region().isBlank()) {
            throw new IllegalArgumentException("REGION_REQUIRED");
        }

        // 1. Appel Riot pour rafraîchir et valider
        RiotAuthPort.ValorantAuthResponse riotResponse = riotAuthPort.refresh(command.refreshToken());

        // 2. Chiffrement du nouveau Refresh Token
        String iv = encryptionService.generateIv();
        String encryptedRefresh = encryptionService.encrypt(riotResponse.refreshToken(), iv);

        // 3. Persistance en base
        long userId = Long.parseLong(currentUserProvider.getCurrentUserId());
        valorantAuthRepository.save(
                userId,
                riotResponse.puuid(),
                command.region(),
                encryptedRefresh,
                iv,
                riotResponse.refreshTokenExpiresAt()
        );

        // 4. On ne retourne que l'Access Token au front
        return new ValorantTokenView(riotResponse.accessToken());
    }
}
