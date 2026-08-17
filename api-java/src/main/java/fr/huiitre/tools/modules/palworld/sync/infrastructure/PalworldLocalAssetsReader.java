package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.HashMap;
import java.util.Map;
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

    // Certains dossiers img/ contiennent à la fois un .png et un .webp pour le même nom de base — seul
    // le .png est réellement servi par assets.tools.huiitre.fr (le .webp renvoie 404, vérifié 2026-08-05
    // sur pal/element/workSuitability). Sans préférence explicite, un Set non-ordonné choisirait l'un ou
    // l'autre au hasard selon le hash des noms de fichiers, cassant l'image pour certaines entrées
    // seulement. Le .webp est mis en premier puis systématiquement écrasé par le .png si présent.
    public Map<String, String> preferredImageFileNameByBaseName(String imgSubDir) {
        Set<String> fileNames = listImageFileNames(imgSubDir);
        Map<String, String> result = new HashMap<>();
        for (String fileName : fileNames) {
            if (fileName.toLowerCase().endsWith(".webp")) result.put(stripExtension(fileName), fileName);
        }
        for (String fileName : fileNames) {
            if (fileName.toLowerCase().endsWith(".png")) result.put(stripExtension(fileName), fileName);
        }
        for (String fileName : fileNames) {
            String base = stripExtension(fileName);
            result.putIfAbsent(base, fileName);
        }
        return result;
    }

    private String stripExtension(String fileName) {
        int dot = fileName.lastIndexOf('.');
        return dot > 0 ? fileName.substring(0, dot) : fileName;
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
