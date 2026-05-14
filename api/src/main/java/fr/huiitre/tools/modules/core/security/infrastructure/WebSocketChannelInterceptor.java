package fr.huiitre.tools.modules.core.security.infrastructure;

import io.jsonwebtoken.Claims;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.messaging.Message;
import org.springframework.messaging.MessageChannel;
import org.springframework.messaging.simp.stomp.StompCommand;
import org.springframework.messaging.simp.stomp.StompHeaderAccessor;
import org.springframework.messaging.support.ChannelInterceptor;
import org.springframework.messaging.support.MessageHeaderAccessor;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.stereotype.Component;

import java.util.Collections;

@Component
public class WebSocketChannelInterceptor implements ChannelInterceptor {

    private static final Logger log = LoggerFactory.getLogger(WebSocketChannelInterceptor.class);
    private final JwtProvider jwtProvider;

    public WebSocketChannelInterceptor(JwtProvider jwtProvider) {
        this.jwtProvider = jwtProvider;
    }

    @Override
    public Message<?> preSend(Message<?> message, MessageChannel channel) {
        StompHeaderAccessor accessor = MessageHeaderAccessor.getAccessor(message, StompHeaderAccessor.class);

        if (accessor != null && StompCommand.CONNECT.equals(accessor.getCommand())) {
            log.info("WebSocket CONNECT attempt detected");
            // Lecture du token depuis le header STOMP 'Authorization' ou un header 'token'
            String token = accessor.getFirstNativeHeader("Authorization");
            if (token != null && token.startsWith("Bearer ")) {
                token = token.substring(7);
            } else {
                token = accessor.getFirstNativeHeader("token");
            }

            if (token != null) {
                try {
                    Claims claims = jwtProvider.parseToken(token);
                    log.info("WebSocket authenticated for user: {}", claims.getSubject());
                    // On définit l'utilisateur dans la session WebSocket
                    UsernamePasswordAuthenticationToken auth = new UsernamePasswordAuthenticationToken(
                            claims.getSubject(),
                            null,
                            Collections.emptyList()
                    );
                    accessor.setUser(auth);
                } catch (Exception e) {
                    log.warn("WebSocket auth failed: {}", e.getMessage());
                    // Si le token est invalide, on laisse l'accessor tel quel (anonyme)
                }
            } else {
                log.warn("No token found in WebSocket CONNECT headers");
            }
        }
        return message;
    }
}
