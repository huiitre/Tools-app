package fr.huiitre.tools.modules.riot.valorant.application.core.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantStorePort;
import fr.huiitre.tools.modules.riot.valorant.application.core.command.LinkValorantAccountCommand;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.RiotAuthPort;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantVersionProvider;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantAccountAuthView;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantAccountView;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class LinkValorantAccountUseCase implements SecuredUseCase {

    private final RiotAuthPort riotAuthPort;
    private final ValorantStorePort valorantStorePort;
    private final ValorantVersionProvider versionProvider;
    private final ValorantAuthService valorantAuthService;
    private final AuthenticatedUserProvider authenticatedUserProvider;

    public LinkValorantAccountUseCase(RiotAuthPort riotAuthPort,
                                       ValorantStorePort valorantStorePort,
                                       ValorantVersionProvider versionProvider,
                                       ValorantAuthService valorantAuthService,
                                       AuthenticatedUserProvider authenticatedUserProvider) {
        this.riotAuthPort = riotAuthPort;
        this.valorantStorePort = valorantStorePort;
        this.versionProvider = versionProvider;
        this.valorantAuthService = valorantAuthService;
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

    public ValorantAccountAuthView execute(LinkValorantAccountCommand command) {
        if (command.refreshToken() == null || command.refreshToken().isBlank()) {
            throw new IllegalArgumentException("REFRESH_TOKEN_REQUIRED");
        }
        if (command.region() == null || command.region().isBlank()) {
            throw new IllegalArgumentException("REGION_REQUIRED");
        }

        Long userId = authenticatedUserProvider.getUserId();

        // 1. Appel Riot pour valider le refresh token et obtenir un access token
        RiotAuthPort.ValorantAuthResponse riotResponse = riotAuthPort.refresh(command.refreshToken());

        // 2. Résolution du pseudo Riot ID (best-effort, ne bloque pas la liaison en cas d'échec)
        String entitlementsToken = valorantStorePort.fetchEntitlementsToken(riotResponse.accessToken());
        String clientVersion = versionProvider.getVersion().get("riotClientVersion").toString();
        ValorantStorePort.RiotId riotId = valorantStorePort.fetchRiotId(
                riotResponse.puuid(), command.region(), riotResponse.accessToken(), entitlementsToken, clientVersion);

        // 3. Persistance via le service (gestion chiffrement centralisée)
        long accountId = valorantAuthService.saveAuthData(
                userId,
                riotResponse.puuid(),
                command.region(),
                riotId.gameName(),
                riotId.tagLine(),
                command.label(),
                riotResponse.refreshToken(),
                riotResponse.refreshTokenExpiresAt()
        );

        ValorantAccountView accountView = new ValorantAccountView(
                accountId, riotResponse.puuid(), command.region(), riotId.gameName(), riotId.tagLine(), command.label());

        return new ValorantAccountAuthView(accountView, riotResponse.accessToken());
    }
}
