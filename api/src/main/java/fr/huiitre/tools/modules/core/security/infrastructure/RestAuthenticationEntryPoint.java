package fr.huiitre.tools.modules.core.security.infrastructure;

import java.io.IOException;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.web.AuthenticationEntryPoint;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

public class RestAuthenticationEntryPoint implements AuthenticationEntryPoint {

    private static final Logger log = LoggerFactory.getLogger(RestAuthenticationEntryPoint.class);

    @Override
    public void commence(
            HttpServletRequest request,
            HttpServletResponse response,
            AuthenticationException authException) throws IOException {

        log.warn(
                "RestAuthenticationEntryPoint ip={} requestURI={}",
                request.getRemoteAddr(),
                request.getRequestURI());

        log.warn(
                "RestAuthenticationEntryPoint contextPath={}",
                request.getContextPath());

        log.warn(
                "RestAuthenticationEntryPoint servletPath={}",
                request.getServletPath());

        log.warn(
                "RestAuthenticationEntryPoint pathInfo={}",
                request.getPathInfo());

        log.warn(
                "RestAuthenticationEntryPoint exception={}",
                authException == null ? "null" : authException.getClass().getName());

        response.setStatus(HttpServletResponse.SC_UNAUTHORIZED);
        response.setContentType("application/json");

        response.getWriter().write("""
                    {
                      "error": "UNAUTHORIZED",
                      "message": "Authentication required or token invalid"
                    }
                """);
    }
}
