package fr.huiitre.tools.modules.riot.valorant.application.core.ports;

import java.time.LocalDateTime;

public interface RiotAuthPort {
    ValorantAuthResponse refresh(String refreshToken);

    record ValorantAuthResponse(String accessToken, String refreshToken, String puuid, LocalDateTime refreshTokenExpiresAt) {}
}
