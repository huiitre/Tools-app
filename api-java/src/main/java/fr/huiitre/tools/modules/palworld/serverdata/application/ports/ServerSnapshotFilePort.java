package fr.huiitre.tools.modules.palworld.serverdata.application.ports;

import java.nio.file.Path;
import java.util.List;

import fr.huiitre.tools.modules.palworld.serverdata.application.ServerSnapshotSyncData;

public interface ServerSnapshotFilePort {

    List<PendingSnapshotFile> listPendingFiles();

    ServerSnapshotSyncData readAndParse(Path file);

    void archive(Path file);
}
