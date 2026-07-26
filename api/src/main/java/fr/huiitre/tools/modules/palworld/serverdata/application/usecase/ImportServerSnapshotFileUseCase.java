package fr.huiitre.tools.modules.palworld.serverdata.application.usecase;

import java.util.Map;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.palworld.serverdata.application.ServerSnapshotSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.PalLookupRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.ServerDataRepository;

// Pas de SecuredUseCase ici : appelée par SyncServerDataUseCase, elle-même invoquée
// par le scheduler (aucun utilisateur authentifié sur un thread de fond).
@Service
@Transactional
public class ImportServerSnapshotFileUseCase {

    private final ServerDataRepository serverDataRepository;
    private final PalLookupRepository palLookupRepository;

    public ImportServerSnapshotFileUseCase(ServerDataRepository serverDataRepository, PalLookupRepository palLookupRepository) {
        this.serverDataRepository = serverDataRepository;
        this.palLookupRepository = palLookupRepository;
    }

    public void execute(String fileName, ServerSnapshotSyncData data) {
        Map<String, Long> palIdByTribeUpper = palLookupRepository.findIdByTribeUpper();
        serverDataRepository.importSnapshot(fileName, data, palIdByTribeUpper);
    }
}
