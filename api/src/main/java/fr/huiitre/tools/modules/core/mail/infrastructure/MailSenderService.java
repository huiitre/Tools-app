package fr.huiitre.tools.modules.core.mail.infrastructure;

import java.io.UnsupportedEncodingException;
import java.nio.file.Path;
import java.util.List;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.mail.javamail.MimeMessageHelper;

import jakarta.mail.MessagingException;
import jakarta.mail.internet.MimeMessage;

public class MailSenderService {

    private final JavaMailSender mailSender;
    private final String from;
    private final List<String> to;

    public MailSenderService(
            JavaMailSender mailSender,
            @Value("${mail.from}") String from,
            @Value("${mail.to}") List<String> to) {
        this.mailSender = mailSender;
        this.from = from;
        this.to = to;
    }

    public void send(String subject, String body) {
        sendInternal(subject, body, null);
    }

    public void sendWithAttachments(
            String subject,
            String body,
            List<Path> attachments) {
        try {
            MimeMessage message = mailSender.createMimeMessage();
            MimeMessageHelper helper = new MimeMessageHelper(message, true);

            try {
                helper.setFrom(from, "Tools - Huiitre.fr");
            } catch (UnsupportedEncodingException e) {
                throw new RuntimeException("Invalid From encoding", e);
            }
            helper.setTo(to.toArray(String[]::new));
            helper.setSubject(subject);
            helper.setText(body, false);

            for (Path attachment : attachments) {
                helper.addAttachment(
                        attachment.getFileName().toString(),
                        attachment.toFile());
            }

            mailSender.send(message);

        } catch (MessagingException e) {
            throw new RuntimeException("Failed to send mail with attachments", e);
        }
    }

    private void sendInternal(String subject, String body, Path attachment) {
        try {
            MimeMessage message = mailSender.createMimeMessage();
            MimeMessageHelper helper = new MimeMessageHelper(message, attachment != null);

            try {
                helper.setFrom(from, "Tools - Huiitre.fr");
            } catch (UnsupportedEncodingException e) {
                throw new RuntimeException("Invalid From encoding", e);
            }
            helper.setTo(to.toArray(String[]::new));
            helper.setSubject(subject);
            helper.setText(body, false);

            if (attachment != null) {
                helper.addAttachment(
                        attachment.getFileName().toString(),
                        attachment.toFile());
            }

            mailSender.send(message);

        } catch (MessagingException e) {
            throw new RuntimeException("Failed to send mail", e);
        }
    }
}
