// config/mail/MailConfig.java
package fr.huiitre.tools.config.mail;

import java.nio.file.Path;
import java.util.List;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.mail.javamail.JavaMailSender;

import fr.huiitre.tools.modules.core.mail.infrastructure.MailSenderService;
import fr.huiitre.tools.modules.core.report.infrastructure.ReportFileGenerator;

@Configuration
public class MailConfig {

    @Bean
    public MailSenderService mailSenderService(
            JavaMailSender javaMailSender,
            @Value("${mail.from}") String from,
            @Value("${mail.to}") List<String> to) {
        return new MailSenderService(javaMailSender, from, to);
    }

    @Bean
    public ReportFileGenerator reportFileGenerator(
            @Value("${report.dir:./tmp/reports}") String reportDir) {
        return new ReportFileGenerator(Path.of(reportDir));
    }
}
