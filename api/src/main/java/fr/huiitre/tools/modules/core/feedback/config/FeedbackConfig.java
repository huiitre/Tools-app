package fr.huiitre.tools.modules.core.feedback.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

import fr.huiitre.tools.modules.core.feedback.application.port.FeedbackRepository;
import fr.huiitre.tools.modules.core.feedback.infrastructure.PostgresFeedbackRepository;

@Configuration
public class FeedbackConfig {

    @Bean
    public FeedbackRepository feedbackRepository(NamedParameterJdbcTemplate jdbcTemplate) {
        return new PostgresFeedbackRepository(jdbcTemplate);
    }
}
