package fr.huiitre.tools.modules.riot.valorant.application.core.usecase;

import fr.huiitre.tools.modules.core.security.infrastructure.EncryptionService;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.RiotAuthPort;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;

@Service
public class ValorantAuthService {

    private final RiotAuthPort riotAuthPort;
    private final ValorantAuthRepository valorantAuthRepository;
    private final EncryptionService encryptionService;

    public ValorantAuthService(RiotAuthPort riotAuthPort,
                               ValorantAuthRepository valorantAuthRepository,
                               EncryptionService encryptionService) {
        this.riotAuthPort = riotAuthPort;
        this.valorantAuthRepository = valorantAuthRepository;
        this.encryptionService = encryptionService;
    }

    public String getOrRefreshAccessToken(Long accountId) {
        // 1. Récupération des données chiffrées en base
        ValorantAuthRepository.ValorantAuthData authData = valorantAuthRepository.findById(accountId)
                .orElseThrow(() -> new IllegalArgumentException("RIOT_AUTH_NOT_FOUND"));

        // 2. Déchiffrement du Refresh Token
        String refreshToken = encryptionService.decrypt(authData.encryptedRefreshToken(), authData.iv());

        try {
            // 3. Appel Riot pour un nouvel Access Token
            RiotAuthPort.ValorantAuthResponse riotResponse = riotAuthPort.refresh(refreshToken);

            // 4. Mise à jour du nouveau Refresh Token (Rotation) - pas de changement de pseudo à ce stade
            String newIv = encryptionService.generateIv();
            String newEncryptedRefresh = encryptionService.encrypt(riotResponse.refreshToken(), newIv);

            valorantAuthRepository.save(
                    authData.userId(),
                    riotResponse.puuid(),
                    authData.region(),
                    null,
                    null,
                    null,
                    newEncryptedRefresh,
                    newIv,
                    riotResponse.refreshTokenExpiresAt()
            );

            return riotResponse.accessToken();

        } catch (IllegalArgumentException e) {
            // Si le refresh token est invalide (périmé chez Riot), on nettoie la base
            if ("RIOT_TOKEN_INVALID".equals(e.getMessage())) {
                valorantAuthRepository.deleteById(accountId);
            }
            throw e;
        }
    }

    public long saveAuthData(Long userId, String puuid, String region, String gameName, String tagLine, String label, String refreshToken, LocalDateTime expiresAt) {
        String iv = encryptionService.generateIv();
        String encryptedRefresh = encryptionService.encrypt(refreshToken, iv);
        return valorantAuthRepository.save(userId, puuid, region, gameName, tagLine, label, encryptedRefresh, iv, expiresAt);
    }
}
