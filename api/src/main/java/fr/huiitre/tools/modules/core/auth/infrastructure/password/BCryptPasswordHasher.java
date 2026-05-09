package fr.huiitre.tools.modules.core.auth.infrastructure.password;

import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;

import fr.huiitre.tools.modules.core.auth.application.ports.PasswordHasher;

public class BCryptPasswordHasher implements PasswordHasher {

    private final BCryptPasswordEncoder encoder = new BCryptPasswordEncoder();

    @Override
    public String hash(String rawPassword) {
        return encoder.encode(rawPassword);
    }

    @Override
    public boolean matches(String rawPassword, String hashedPassword) {
        return encoder.matches(rawPassword, hashedPassword);
    }
}
