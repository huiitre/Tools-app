package fr.huiitre.tools.config;

import io.swagger.v3.oas.annotations.OpenAPIDefinition;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.context.annotation.Configuration;

@Configuration
@OpenAPIDefinition(
    tags = {
        @Tag(name = "Core - Auth"),
        @Tag(name = "Core - Module"),
        @Tag(name = "Core - Role"),
        @Tag(name = "Health - Weight Log"),
        @Tag(name = "Core - Test")
    }
)
public class OpenApiConfig {
}