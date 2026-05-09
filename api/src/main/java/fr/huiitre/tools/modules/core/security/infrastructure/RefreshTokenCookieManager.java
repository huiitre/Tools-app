package fr.huiitre.tools.modules.core.security.infrastructure;

import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseCookie;
import org.springframework.stereotype.Component;
import jakarta.servlet.http.HttpServletResponse;

@Component
public class RefreshTokenCookieManager {

    private static final String COOKIE_NAME = "refresh_token";
    private final SecurityCookieProperties cookieProperties;

    public RefreshTokenCookieManager(SecurityCookieProperties cookieProperties) {
        this.cookieProperties = cookieProperties;
    }

    public void set(HttpServletResponse response, String refreshToken, int maxAgeSeconds) {
        boolean isSecure = cookieProperties.isSecure();
        
        ResponseCookie cookie = ResponseCookie.from(COOKIE_NAME, refreshToken)
                .httpOnly(true)
                .secure(isSecure)
                .path("/")
                .maxAge(maxAgeSeconds)
                .sameSite(isSecure ? "None" : "Lax")
                .build();

        response.addHeader(HttpHeaders.SET_COOKIE, cookie.toString());
    }

    public void clear(HttpServletResponse response) {
        boolean isSecure = cookieProperties.isSecure();
        
        ResponseCookie cookie = ResponseCookie.from(COOKIE_NAME, "")
                .httpOnly(true)
                .secure(isSecure)
                .path("/")
                .maxAge(0)
                .sameSite(isSecure ? "None" : "Lax")
                .build();

        response.addHeader(HttpHeaders.SET_COOKIE, cookie.toString());
    }
}
