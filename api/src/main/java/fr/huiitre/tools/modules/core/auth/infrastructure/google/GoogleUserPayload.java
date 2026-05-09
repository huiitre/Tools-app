package fr.huiitre.tools.modules.core.auth.infrastructure.google;

public class GoogleUserPayload {

    private final String providerUserId; // sub
    private final String email;
    private final String name;
    private final String picture;

    public GoogleUserPayload(String providerUserId, String email, String name, String picture) {
        this.providerUserId = providerUserId;
        this.email = email;
        this.name = name;
        this.picture = picture;
    }

    public String getProviderUserId() {
        return providerUserId;
    }

    public String getEmail() {
        return email;
    }

    public String getName() {
        return name;
    }

    public String getPicture() {
        return picture;
    }
}
