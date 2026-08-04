package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.Set;
import java.util.stream.Collectors;
import java.util.stream.Stream;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import jakarta.annotation.PostConstruct;

@Component
public class PalworldLocalAssetsReader {

    private final ObjectMapper objectMapper = new ObjectMapper();

    private Path palworldRoot;

    @Value("${tools.assets.base-path}")
    private Path assetsBasePath;

    @PostConstruct
    void init() {
        if (assetsBasePath == null) {
            throw new IllegalStateException("tools.assets.base-path is not configured");
        }
        palworldRoot = assetsBasePath.resolve("tools_palworld/palworld");
    }

    public String readFile(String relativePath) {
        Path file = palworldRoot.resolve(relativePath);
        try {
            return Files.readString(file);
        } catch (IOException e) {
            throw new IllegalStateException("Unable to read Palworld asset file: " + file, e);
        }
    }

    public Set<String> listImageFileNames(String imgSubDir) {
        Path dir = palworldRoot.resolve("img").resolve(imgSubDir);
        try (Stream<Path> stream = Files.list(dir)) {
            return stream.map(p -> p.getFileName().toString()).collect(Collectors.toSet());
        } catch (IOException e) {
            throw new IllegalStateException("Unable to list Palworld image directory: " + dir, e);
        }
    }

    public OffsetDateTime readScrapedAt() {
        try {
            JsonNode root = objectMapper.readTree(readFile("version.json"));
            // "scrapedAt" (ancien extracteur scraper) a été remplacé par "generatedAt" (extracteur pak).
            return Instant.parse(root.path("generatedAt").asText()).atOffset(ZoneOffset.UTC);
        } catch (Exception e) {
            throw new IllegalStateException("Unable to read Palworld version.json generatedAt", e);
        }
    }
}
