package fr.huiitre.tools.modules.dofus.sync.infrastructure.retro;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * Lecture brute des assets locaux Dofus Retro.
 */
@Component
public class RetroLocalAssetsReader {

    private Path retroRoot;

    @Value("${tools.assets.base-path}")
    private Path assetsBasePath;

    @PostConstruct
    void init() {
        if (assetsBasePath == null) {
            throw new IllegalStateException("tools.assets.base-path is not configured");
        }
        // Chemin vers le dossier retro extrait
        this.retroRoot = assetsBasePath.resolve("tools_dofus/retro");
    }

    public String readFile(String fileName) {
        Path file = retroRoot.resolve(fileName);
        try {
            return Files.readString(file);
        } catch (IOException e) {
            throw new IllegalStateException("Unable to read Retro asset file: " + file, e);
        }
    }
}