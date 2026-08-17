package fr.huiitre.tools.modules.elite_dangerous.r2r.infrastructure.importer;

import java.io.IOException;
import java.nio.charset.StandardCharsets;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.elite_dangerous.r2r.application.importer.RouteImporter;

public class SpanshJsonRouteImporter implements RouteImporter {

    private static final ObjectMapper MAPPER = new ObjectMapper();

    @Override
    public String source() {
        return "spansh";
    }

    @Override
    public String parse(byte[] fileContent, String fileName) throws IOException {
        String json = new String(fileContent, StandardCharsets.UTF_8);
        JsonNode root = MAPPER.readTree(json);
        if (!root.has("result") || !root.get("result").isArray()) {
            throw new IllegalArgumentException("R2R_INVALID_JSON_FORMAT");
        }
        return json;
    }
}
