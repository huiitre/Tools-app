package fr.huiitre.tools.config.todolist;

import javax.sql.DataSource;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.todolist.application.ports.TodoRepository;
import fr.huiitre.tools.modules.todolist.application.ports.TodolistRepository;
import fr.huiitre.tools.modules.todolist.infrastructure.PostgresTodoRepository;
import fr.huiitre.tools.modules.todolist.infrastructure.PostgresTodolistRepository;

@Configuration
public class TodolistConfig {
    
    @Bean
    public TodolistRepository todolistRepository(
        JdbcTemplate jdbcTemplate
    ) {
        return new PostgresTodolistRepository(
            jdbcTemplate
        );
    }

    @Bean
    public TodoRepository todoRepository(
        JdbcTemplate jdbcTemplate
    ) {
        return new PostgresTodoRepository(
            jdbcTemplate
        );
    }
}
