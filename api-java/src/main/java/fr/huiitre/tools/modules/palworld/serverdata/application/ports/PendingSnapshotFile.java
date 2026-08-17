package fr.huiitre.tools.modules.palworld.serverdata.application.ports;

import java.nio.file.Path;

public record PendingSnapshotFile(Path path, String fileName) {}
