package fr.huiitre.tools.modules.core.auth.infrastructure.mail;

import java.io.UnsupportedEncodingException;

import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.mail.javamail.MimeMessageHelper;

import fr.huiitre.tools.modules.core.auth.application.ports.EmailSender;
import jakarta.mail.MessagingException;
import jakarta.mail.internet.MimeMessage;

public class AuthMailSenderService implements EmailSender {

    private static final String FROM = "admin@huiitre.fr";

    private final JavaMailSender mailSender;

    public AuthMailSenderService(
            JavaMailSender mailSender) {
        this.mailSender = mailSender;
    }

    public void sendEmailVerification(
            String toEmail,
            String verificationLink) {
        try {
            MimeMessage message = mailSender.createMimeMessage();
            MimeMessageHelper helper = new MimeMessageHelper(message, false, "UTF-8");

            helper.setFrom(FROM, "Tools - Huiitre");
            helper.setTo(toEmail);
            helper.setSubject("Vérification de votre adresse email");
            helper.setText(
                    """
                            Bonjour,

                            Merci de confirmer votre adresse email en cliquant sur le lien suivant :

                            %s

                            Ce lien expire dans 30 minutes.
                            """.formatted(verificationLink),
                    false);

            mailSender.send(message);

        } catch (MessagingException | UnsupportedEncodingException e) {
            throw new RuntimeException("L’envoi de l’email de confirmation a échoué. Veuillez réessayer plus tard.", e);
        }
    }

    @Override
    public void sendPasswordReset(String toEmail, String resetLink) {
        try {
            MimeMessage message = mailSender.createMimeMessage();
            MimeMessageHelper helper = new MimeMessageHelper(message, false, "UTF-8");

            helper.setFrom(FROM, "Tools - Huiitre");
            helper.setTo(toEmail);
            helper.setSubject("Réinitialisation de votre mot de passe");
            helper.setText(
                    """
                            Bonjour,

                            Une demande de réinitialisation de mot de passe a été effectuée.
                            Pour définir un nouveau mot de passe, cliquez sur le lien suivant :

                            %s

                            Ce lien expire dans 30 minutes.
                            Si vous n’êtes pas à l’origine de cette demande, ignorez cet email.
                            """.formatted(resetLink),
                    false);

            mailSender.send(message);

        } catch (MessagingException | UnsupportedEncodingException e) {
            throw new RuntimeException(
                    "L’envoi de l’email de réinitialisation du mot de passe a échoué. Veuillez réessayer plus tard.",
                    e);
        }
    }

}
