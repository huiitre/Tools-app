package fr.huiitre.tools.modules.riot.valorant.application.core.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.security.infrastructure.EncryptionService;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.RiotAuthPort;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantTokenView;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class GetValorantAccessTokenUseCase implements SecuredUseCase {

    private final RiotAuthPort riotAuthPort;
    private final ValorantAuthRepository valorantAuthRepository;
    private final EncryptionService encryptionService;
    private final CurrentUserProvider currentUserProvider;

    public GetValorantAccessTokenUseCase(RiotAuthPort riotAuthPort,
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

    public ValorantTokenView execute() {
        long userId = Long.parseLong(currentUserProvider.getCurrentUserId());
        
        // 1. Récupération des données chiffrées en base
        ValorantAuthRepository.ValorantAuthData authData = valorantAuthRepository.findByUserId(userId)
                .orElseThrow(() -> new IllegalArgumentException("RIOT_AUTH_NOT_FOUND"));

        // 2. Déchiffrement du Refresh Token
        String refreshToken = encryptionService.decrypt(authData.encryptedRefreshToken(), authData.iv());

        try {
            // 3. Appel Riot pour un nouvel Access Token
            RiotAuthPort.ValorantAuthResponse riotResponse = riotAuthPort.refresh(refreshToken);

            // 4. Mise à jour du nouveau Refresh Token (Rotation)
            String newIv = encryptionService.generateIv();
            String newEncryptedRefresh = encryptionService.encrypt(riotResponse.refreshToken(), newIv);
            
            valorantAuthRepository.save(
                    userId,
                    riotResponse.puuid(),
                    authData.region(),
                    newEncryptedRefresh,
                    newIv,
                    riotResponse.refreshTokenExpiresAt()
            );

            return new ValorantTokenView(riotResponse.accessToken());
            
        } catch (IllegalArgumentException e) {
            // Si le refresh token est invalide (périmé chez Riot), on nettoie la base
            if ("RIOT_TOKEN_INVALID".equals(e.getMessage())) {
                valorantAuthRepository.deleteByUserId(userId);
            }
            throw e;
        }
    }
}
