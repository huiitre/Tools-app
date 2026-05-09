package fr.huiitre.tools.modules.core.security.infrastructure;

import java.nio.charset.StandardCharsets;
import java.util.Date;
import java.util.Map;

import javax.crypto.SecretKey;

import org.springframework.stereotype.Component;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;

@Component
public class JwtProvider {

    private final JwtProperties props;
    private final SecretKey secretKey;

    public JwtProvider(JwtProperties props) {
        this.props = props;
        this.secretKey = Keys.hmacShaKeyFor(
                props.getSecret().getBytes(StandardCharsets.UTF_8));
    }

    /*
     * =========================
     * ACCESS TOKEN
     * =========================
     */
    public String generateAccessToken(String subject, Map<String, Object> claims) {
        Date now = new Date();
        Date expiry = new Date(now.getTime() + props.getAccessTokenTtl() * 1000);

        return Jwts.builder()
                .issuer(props.getIssuer())
                .subject(subject)
                .claims(claims)
                .issuedAt(now)
                .expiration(expiry)
                .signWith(secretKey)
                .compact();
    }

    /*
     * =========================
     * REFRESH TOKEN
     * =========================
     */
    public String generateRefreshToken(String subject, Date expiration) {
        Date now = new Date();

        return Jwts.builder()
                .issuer(props.getIssuer())
                .subject(subject)
                .claim("tokenType", "REFRESH")
                .issuedAt(now)
                .expiration(expiration)
                .signWith(secretKey)
                .compact();
    }

    public String generateRefreshToken(String subject) {
        Date now = new Date();
        Date expiration = new Date(
                now.getTime() + props.getRefreshTokenTtl() * 1000);
        return generateRefreshToken(subject, expiration);
    }
    /*
     * =========================
     * VALIDATION
     * =========================
     */

    public Claims parseToken(String token) {
        return Jwts.parser()
                .verifyWith(secretKey)
                .requireIssuer(props.getIssuer())
                .build()
                .parseSignedClaims(token)
                .getPayload();
    }
}
