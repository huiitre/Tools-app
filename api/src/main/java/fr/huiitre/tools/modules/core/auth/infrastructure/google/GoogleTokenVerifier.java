package fr.huiitre.tools.modules.core.auth.infrastructure.google;

import java.net.URI;
import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;

import fr.huiitre.tools.modules.core.auth.application.exception.InvalidCredentialsException;

@Service
public class GoogleTokenVerifier {

    private static final String GOOGLE_TOKEN_INFO = "https://oauth2.googleapis.com/tokeninfo?id_token=%s";

    private static final Logger logger = LoggerFactory.getLogger(GoogleTokenVerifier.class);

    private final RestTemplate restTemplate = new RestTemplate();

    @SuppressWarnings("unchecked")
    public GoogleUserPayload verify(String idToken) {
        try {
            Map<String, Object> payload = restTemplate.getForObject(
                    URI.create(String.format(GOOGLE_TOKEN_INFO, idToken)),
                    Map.class);

            String sub = (String) payload.get("sub");
            String email = (String) payload.get("email");
            String name = (String) payload.get("name");
            String picture = (String) payload.get("picture");

            if (sub == null || email == null) {
                throw new InvalidCredentialsException();
            }

            return new GoogleUserPayload(sub, email, name, picture);

        } catch (Exception e) {
            throw new InvalidCredentialsException();
        }
    }
}
