package fr.huiitre.tools.modules.riot.valorant.application.core.command;

public class RefreshTokenCommand {

    private final String refreshToken;

    public RefreshTokenCommand(String refreshToken) {
        this.refreshToken = refreshToken;
    }

    public String getRefreshToken() {
        return refreshToken;
    }
}
