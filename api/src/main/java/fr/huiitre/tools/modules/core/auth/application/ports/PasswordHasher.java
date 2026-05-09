package fr.huiitre.tools.modules.core.auth.application.ports;

public interface PasswordHasher {

    String hash(String rawPassword);

    boolean matches(String rawPassword, String hashedPassword);
}
