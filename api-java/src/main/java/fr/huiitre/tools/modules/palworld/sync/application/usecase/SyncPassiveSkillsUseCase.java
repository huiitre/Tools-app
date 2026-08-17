package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.nio.file.NoSuchFileException;
import java.util.HashSet;
import java.util.List;
import java.util.Optional;
import java.util.Set;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.PassiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PassiveSkillDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PassiveSkillSyncRepository;

@Service
@Transactional
public class SyncPassiveSkillsUseCase implements SecuredUseCase {

    private final PassiveSkillDataProvider dataProvider;
    private final PassiveSkillSyncRepository syncRepository;

    public SyncPassiveSkillsUseCase(
            PassiveSkillDataProvider dataProvider,
            PassiveSkillSyncRepository syncRepository) {
        this.dataProvider = dataProvider;
        this.syncRepository = syncRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public PalworldSyncReport execute() {
        final List<PassiveSkillSyncData> external;
        try {
            external = dataProvider.fetchDisplayable();
        } catch (IllegalStateException exception) {
            // The passive catalog is an optional local asset.  A missing catalog must
            // not make the complete Palworld synchronization fail (and must not
            // delete the catalog already stored in the database).
            if (causedByMissingFile(exception)) {
                return new PalworldSyncReport(0, 0, 0);
            }
            throw exception;
        }
        Set<String> currentIds = new HashSet<>(syncRepository.findAllIds());
        Set<String> externalIds = new HashSet<>();
        int created = 0;
        int updated = 0;

        for (PassiveSkillSyncData passiveSkill : external) {
            externalIds.add(passiveSkill.id());
            if (currentIds.contains(passiveSkill.id())) updated++;
            else created++;
            syncRepository.upsert(passiveSkill);
        }

        int deleted = 0;
        for (String currentId : currentIds) {
            if (!externalIds.contains(currentId)) {
                syncRepository.deleteById(currentId);
                deleted++;
            }
        }

        return new PalworldSyncReport(created, updated, deleted);
    }

    private boolean causedByMissingFile(Throwable exception) {
        Throwable current = exception;
        while (current != null) {
            if (current instanceof NoSuchFileException) return true;
            current = current.getCause();
        }
        return false;
    }
}
