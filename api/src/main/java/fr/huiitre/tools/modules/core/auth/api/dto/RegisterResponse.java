package fr.huiitre.tools.modules.core.auth.api.dto;

public class RegisterResponse {
    
    private final String status;
    private final String message;

    public RegisterResponse(
        String status,
        String message
    ) {
        this.status = status;
        this.message = message;
    }

    public String getStatus() {
      return this.status;
    }

    public String getMessage() {
      return this.message;
    }
}
