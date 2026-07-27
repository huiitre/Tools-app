package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.util.HashMap;
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
import fr.huiitre.tools.modules.palworld.sync.application.WorkSuitabilitySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.WorkSuitabilitySyncResult;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkSuitabilityDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkSuitabilitySyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.WorkSuitabilityRefView;

@Service
@Transactional
public class SyncWorkSuitabilitiesUseCase implements SecuredUseCase {

    private final WorkSuitabilityDataProvider dataProvider;
    private final WorkSuitabilitySyncRepository syncRepository;

    public SyncWorkSuitabilitiesUseCase(WorkSuitabilityDataProvider dataProvider, WorkSuitabilitySyncRepository syncRepository) {
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

    public WorkSuitabilitySyncResult execute() {
        List<WorkSuitabilitySyncData> external = dataProvider.fetchAll();

        // Matching sur external_code (id stable côté paldb.cc, ex: "03"), pas sur slug : le slug peut
        // changer de format d'un scrape à l'autre (ex: "GeneratingElectricity" -> "Generating_Electricity"),
        // alors qu'external_code, lui, ne bouge jamais pour une même entrée.
        Map<String, WorkSuitabilityRefView> currentByExternalCode = syncRepository.findAll().stream()
                .collect(Collectors.toMap(WorkSuitabilityRefView::externalCode, it -> it));

        Set<String> externalCodes = external.stream()
                .map(WorkSuitabilitySyncData::getExternalCode)
                .collect(Collectors.toSet());

        Map<String, Long> idBySlug = new HashMap<>();
        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (WorkSuitabilitySyncData ext : external) {
            WorkSuitabilityRefView existing = currentByExternalCode.get(ext.getExternalCode());

            if (existing == null) {
                Long newId = syncRepository.save(ext);
                idBySlug.put(ext.getSlug(), newId);
                created++;
                continue;
            }

            idBySlug.put(ext.getSlug(), existing.id());

            boolean changed = !Objects.equals(existing.slug(), ext.getSlug())
                    || !Objects.equals(existing.name(), ext.getName())
                    || !Objects.equals(existing.iconUrl(), ext.getIconUrl());

            if (changed) {
                syncRepository.update(existing.id(), ext);
                updated++;
            }
        }

        for (WorkSuitabilityRefView current : currentByExternalCode.values()) {
            if (!externalCodes.contains(current.externalCode())) {
                syncRepository.delete(current.id());
                deleted++;
            }
        }

        return new WorkSuitabilitySyncResult(new PalworldSyncReport(created, updated, deleted), idBySlug);
    }
}
