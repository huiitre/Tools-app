// infrastructure/report/ReportFileGenerator.java
package fr.huiitre.tools.modules.core.report.infrastructure;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

public class ReportFileGenerator {

    private final Path baseDir;

    public ReportFileGenerator(Path baseDir) {
        this.baseDir = baseDir;
    }

    public Path generate(String filename, String content) {
        try {
            Files.createDirectories(baseDir);
            Path file = baseDir.resolve(filename);
            Files.writeString(file, content, StandardCharsets.UTF_8);
            return file;
        } catch (IOException e) {
            throw new IllegalStateException("Failed to generate report file: " + filename, e);
        }
    }
}
