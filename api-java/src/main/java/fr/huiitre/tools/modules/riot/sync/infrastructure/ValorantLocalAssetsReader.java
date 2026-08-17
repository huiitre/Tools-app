package fr.huiitre.tools.modules.riot.sync.infrastructure;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

@Component
public class ValorantLocalAssetsReader {

    private static Path VALORANT_ROOT;

    @Value("${tools.assets.base-path}")
    private Path assetsBasePath;

    @PostConstruct
    void init() {
        if (assetsBasePath == null) {
            throw new IllegalStateException("tools.assets.base-path is not configured");
        }
        VALORANT_ROOT = assetsBasePath.resolve("tools_riot/valorant");
    }

    public String readFile(String relativePath) {
        Path file = VALORANT_ROOT.resolve(relativePath);
        try {
            return Files.readString(file);
        } catch (IOException e) {
            throw new IllegalStateException("Unable to read Valorant asset file: " + file, e);
        }
    }
}
