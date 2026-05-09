package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * Lecture brute des assets locaux Dofus3.
 *
 * - aucun mapping
 * - aucun DTO
 * - aucune logique métier
 *
 * Ce fichier sert UNIQUEMENT à lire des fichiers depuis le disque.
 */
@Component
public class Dofus3LocalAssetsReader {

    private static Path DOFUS3_ROOT;

    @Value("${tools.assets.base-path}")
    private Path assetsBasePath;

    @PostConstruct
    void init() {
        if (assetsBasePath == null) {
            throw new IllegalStateException("tools.assets.base-path is not configured");
        }

        DOFUS3_ROOT = assetsBasePath.resolve("tools_dofus/dofus3");
    }

    public String readFile(String relativePath) {
        Path file = DOFUS3_ROOT.resolve(relativePath);

        try {
            return Files.readString(file);
        } catch (IOException e) {
            throw new IllegalStateException(
                    "Unable to read Dofus3 asset file: " + file,
                    e);
        }
    }
}
