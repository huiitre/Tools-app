package fr.huiitre.tools.modules.core.auth.infrastructure.google;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.NimbusJwtDecoder;
import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.auth.application.exception.InvalidCredentialsException;

@Service
public class GoogleTokenVerifier {

    private static final String GOOGLE_JWK_URI = "https://www.googleapis.com/oauth2/v3/certs";
    private static final String GOOGLE_ISSUER = "https://accounts.google.com";

    @Value("${google.oauth.client-id}")
    private String clientId;

    private final JwtDecoder decoder = NimbusJwtDecoder.withJwkSetUri(GOOGLE_JWK_URI).build();

    public GoogleUserPayload verify(String idToken) {
        try {
            Jwt jwt = decoder.decode(idToken);

            if (!jwt.getAudience().contains(clientId)) {
                throw new InvalidCredentialsException();
            }

            if (jwt.getIssuer() == null || !GOOGLE_ISSUER.equals(jwt.getIssuer().toString())) {
                throw new InvalidCredentialsException();
            }

            String sub = jwt.getSubject();
            String email = jwt.getClaimAsString("email");
            String name = jwt.getClaimAsString("name");
            String picture = jwt.getClaimAsString("picture");

            if (sub == null || email == null) {
                throw new InvalidCredentialsException();
            }

            return new GoogleUserPayload(sub, email, name, picture);

        } catch (InvalidCredentialsException e) {
            throw e;
        } catch (Exception e) {
            throw new InvalidCredentialsException();
        }
    }
}