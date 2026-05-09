package fr.huiitre.tools.modules.core.auth.application.ports;

public interface EmailSender {

    void sendEmailVerification(
            String toEmail,
            String verificationLink);

    void sendPasswordReset(String toEmail, String resetLink);
}
