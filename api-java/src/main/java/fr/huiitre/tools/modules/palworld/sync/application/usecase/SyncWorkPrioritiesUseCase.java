package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.WorkPrioritySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.WorkPrioritySyncResult;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkPriorityDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkPrioritySyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.WorkPriorityRefView;

@Service
@Transactional
public class SyncWorkPrioritiesUseCase implements SecuredUseCase {

    private final WorkPriorityDataProvider dataProvider;
    private final WorkPrioritySyncRepository syncRepository;

    public SyncWorkPrioritiesUseCase(WorkPriorityDataProvider dataProvider, WorkPrioritySyncRepository syncRepository) {
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

    public WorkPrioritySyncResult execute(Map<String, Long> workSuitabilityIdBySlug) {
        List<WorkPrioritySyncData> external = dataProvider.fetchAll();

        Map<String, WorkPriorityRefView> currentByCode = syncRepository.findAll().stream()
                .collect(Collectors.toMap(WorkPriorityRefView::code, it -> it));

        Set<String> externalCodes = external.stream()
                .map(WorkPrioritySyncData::getCode)
                .collect(Collectors.toSet());

        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (WorkPrioritySyncData ext : external) {
            Long workSuitabilityId = ext.getWorkSuitabilitySlug() == null
                    ? null : workSuitabilityIdBySlug.get(ext.getWorkSuitabilitySlug());
            WorkPriorityRefView existing = currentByCode.get(ext.getCode());

            if (existing == null) {
                syncRepository.save(ext, workSuitabilityId);
                created++;
                continue;
            }

            boolean changed = !Objects.equals(existing.name(), ext.getName())
                    || !Objects.equals(existing.iconUrl(), ext.getIconUrl())
                    || !Objects.equals(existing.workSuitabilityId(), workSuitabilityId)
                    || existing.priority() != ext.getPriority();

            if (changed) {
                syncRepository.update(existing.id(), ext, workSuitabilityId);
                updated++;
            }
        }

        for (WorkPriorityRefView current : currentByCode.values()) {
            if (!externalCodes.contains(current.code())) {
                syncRepository.delete(current.id());
                deleted++;
            }
        }

        return new WorkPrioritySyncResult(new PalworldSyncReport(created, updated, deleted));
    }
}
