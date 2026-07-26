package fr.huiitre.tools.modules.palworld.serverdata.application.usecase;

import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.palworld.serverdata.application.ServerDataSyncReport;
import fr.huiitre.tools.modules.palworld.serverdata.application.ServerSnapshotSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.PendingSnapshotFile;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.ServerDataRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.ServerSnapshotFilePort;

// Pas de SecuredUseCase ici : cette classe est appelée à la fois par le scheduler
// (aucun utilisateur authentifié sur un thread de fond) et par le endpoint admin manuel.
@Service
public class SyncServerDataUseCase {

    private static final Logger log = LoggerFactory.getLogger(SyncServerDataUseCase.class);

    private final ServerSnapshotFilePort filePort;
    private final ServerDataRepository serverDataRepository;
    private final ImportServerSnapshotFileUseCase importServerSnapshotFileUseCase;

    public SyncServerDataUseCase(
            ServerSnapshotFilePort filePort,
            ServerDataRepository serverDataRepository,
            ImportServerSnapshotFileUseCase importServerSnapshotFileUseCase) {
        this.filePort = filePort;
        this.serverDataRepository = serverDataRepository;
        this.importServerSnapshotFileUseCase = importServerSnapshotFileUseCase;
    }

    public ServerDataSyncReport execute() {
        List<PendingSnapshotFile> pendingFiles = filePort.listPendingFiles();

        int imported = 0;
        int alreadyImportedButNotMoved = 0;
        int failed = 0;

        for (PendingSnapshotFile pending : pendingFiles) {
            if (serverDataRepository.isFileAlreadyImported(pending.fileName())) {
                log.warn("Snapshot file {} already imported but still present, moving without reprocessing", pending.fileName());
                filePort.archive(pending.path());
                alreadyImportedButNotMoved++;
                continue;
            }

            try {
                ServerSnapshotSyncData data = filePort.readAndParse(pending.path());
                importServerSnapshotFileUseCase.execute(pending.fileName(), data);
                filePort.archive(pending.path());
                imported++;
            } catch (Exception e) {
                log.error("Failed to import snapshot file {}, leaving it in place for next run", pending.fileName(), e);
                failed++;
            }
        }

        return new ServerDataSyncReport(imported, alreadyImportedButNotMoved, failed);
    }
}
