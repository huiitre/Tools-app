package fr.huiitre.tools.modules.core.auth.api.dto;

public class LoginResponse {

    private final String accessToken;
    private final String tokenType;

    public LoginResponse(String accessToken) {
        this.accessToken = accessToken;
        this.tokenType = "Bearer";
    }

    public String getAccessToken() {
      return accessToken;
    }

    public String getTokenType() {
      return tokenType;
    }
}
