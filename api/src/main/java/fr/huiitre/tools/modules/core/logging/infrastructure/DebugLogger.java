package fr.huiitre.tools.modules.core.logging.infrastructure;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.fasterxml.jackson.databind.ObjectMapper;

public final class DebugLogger {

    private static final ObjectMapper MAPPER = new ObjectMapper().findAndRegisterModules();

    private final Logger logger;

    private DebugLogger(Class<?> type) {
        this.logger = LoggerFactory.getLogger(type);
    }

    public static DebugLogger of(Class<?> type) {
        return new DebugLogger(type);
    }

    public void debug(String message, Object value) {
        if (!logger.isDebugEnabled())
            return;
        logger.debug(message, toJson(value));
    }

    public void info(String message, Object value) {
        if (!logger.isInfoEnabled())
            return;
        logger.info(message, toJson(value));
    }

    private static String toJson(Object o) {
        try {
            return MAPPER
                    .writerWithDefaultPrettyPrinter()
                    .writeValueAsString(o);
        } catch (Exception e) {
            return "[DEBUG_JSON_ERROR] " + e.getMessage();
        }
    }
}