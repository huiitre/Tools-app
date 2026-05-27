package fr.huiitre.tools.modules.core.auth.infrastructure.google;

import java.time.Instant;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.auth.application.exception.InvalidCredentialsException;

@Service
public class GoogleOAuthStateService {

    private final ConcurrentHashMap<String, StateEntry> pending = new ConcurrentHashMap<>();

    public String generate(String source) {
        cleanup();
        String nonce = UUID.randomUUID().toString();
        pending.put(nonce, new StateEntry(source, Instant.now().plusSeconds(600)));
        return nonce;
    }

    public String validateAndGetSource(String state) {
        StateEntry entry = pending.remove(state);
        if (entry == null || Instant.now().isAfter(entry.expiresAt())) {
            throw new InvalidCredentialsException();
        }
        return entry.source();
    }

    private void cleanup() {
        pending.entrySet().removeIf(e -> Instant.now().isAfter(e.getValue().expiresAt()));
    }

    private record StateEntry(String source, Instant expiresAt) {}
}