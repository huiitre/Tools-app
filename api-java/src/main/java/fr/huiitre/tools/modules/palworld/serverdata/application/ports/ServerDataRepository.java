package fr.huiitre.tools.modules.palworld.serverdata.application.ports;

import java.util.Map;

import fr.huiitre.tools.modules.palworld.serverdata.application.ServerSnapshotSyncData;

public interface ServerDataRepository {

    boolean isFileAlreadyImported(String fileName);

    void importSnapshot(String fileName, ServerSnapshotSyncData data, Map<String, Long> palIdByTribeUpper);
}
