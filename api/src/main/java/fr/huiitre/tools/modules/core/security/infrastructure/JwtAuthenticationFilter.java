package fr.huiitre.tools.modules.core.security.infrastructure;

import java.io.IOException;
import java.util.Collections;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.filter.OncePerRequestFilter;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.JwtException;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

public class JwtAuthenticationFilter extends OncePerRequestFilter {

    private final JwtProvider jwtProvider;
    private static final Logger log = LoggerFactory.getLogger(JwtAuthenticationFilter.class);

    public JwtAuthenticationFilter(JwtProvider jwtProvider) {
        this.jwtProvider = jwtProvider;
    }

    @Override
    protected void doFilterInternal(
            HttpServletRequest request,
            HttpServletResponse response,
            FilterChain filterChain) throws ServletException, IOException {

        log.warn("JWT_FILTER uri={} method={} authHeader={}",
                request.getRequestURI(),
                request.getMethod(),
                request.getHeader("Authorization"));

        String authHeader = request.getHeader("Authorization");

        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);

            try {
                Claims claims = jwtProvider.parseToken(token);

                Boolean isActive = claims.get("isActive", Boolean.class);
                if (isActive == null || !isActive) {
                    log.warn(
                            "AUTH_BLOCKED_DISABLED_USER ip={} uri={} sub={}",
                            request.getRemoteAddr(),
                            request.getRequestURI(),
                            claims.getSubject());
                    SecurityContextHolder.clearContext();
                    filterChain.doFilter(request, response);
                    return;
                }

                UsernamePasswordAuthenticationToken authentication = new UsernamePasswordAuthenticationToken(
                        claims.getSubject(),
                        null,
                        Collections.emptyList());

                SecurityContextHolder.getContext().setAuthentication(authentication);

            } catch (ExpiredJwtException e) {

                log.warn(
                        "AUTH_TOKEN_EXPIRED ip={} uri={}",
                        request.getRemoteAddr(),
                        request.getRequestURI());
                SecurityContextHolder.clearContext();

            } catch (JwtException | IllegalArgumentException e) {

                log.warn(
                        "AUTH_TOKEN_INVALID ip={} uri={} reason={}",
                        request.getRemoteAddr(),
                        request.getRequestURI(),
                        e.getClass().getSimpleName());
                SecurityContextHolder.clearContext();
            }
        }

        filterChain.doFilter(request, response);
    }
}
