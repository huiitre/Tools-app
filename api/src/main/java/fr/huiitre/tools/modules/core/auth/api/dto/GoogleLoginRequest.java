package fr.huiitre.tools.modules.core.auth.api.dto;

import jakarta.validation.constraints.NotBlank;

public class GoogleLoginRequest {

    @NotBlank
    private String idToken;

    public String getIdToken() {
        return idToken;
    }
}
