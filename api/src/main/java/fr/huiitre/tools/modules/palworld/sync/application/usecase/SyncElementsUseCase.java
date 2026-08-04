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
import fr.huiitre.tools.modules.palworld.sync.application.ElementSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ElementSyncResult;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ElementDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ElementSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.ElementRefView;

@Service
@Transactional
public class SyncElementsUseCase implements SecuredUseCase {

    private final ElementDataProvider dataProvider;
    private final ElementSyncRepository syncRepository;

    public SyncElementsUseCase(ElementDataProvider dataProvider, ElementSyncRepository syncRepository) {
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

    public ElementSyncResult execute() {
        List<ElementSyncData> external = dataProvider.fetchAll();

        Map<String, ElementRefView> currentByCode = syncRepository.findAll().stream()
                .collect(Collectors.toMap(ElementRefView::externalCode, it -> it));

        Set<String> externalCodes = external.stream()
                .map(ElementSyncData::getExternalCode)
                .collect(Collectors.toSet());

        Map<String, Long> idByExternalCode = new HashMap<>();
        Map<String, Long> idByName = new HashMap<>();
        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (ElementSyncData ext : external) {
            ElementRefView existing = currentByCode.get(ext.getExternalCode());

            if (existing == null) {
                Long newId = syncRepository.save(ext);
                idByExternalCode.put(ext.getExternalCode(), newId);
                idByName.put(ext.getName(), newId);
                created++;
                continue;
            }

            idByExternalCode.put(ext.getExternalCode(), existing.id());
            idByName.put(ext.getName(), existing.id());

            boolean changed = !Objects.equals(existing.name(), ext.getName())
                    || !Objects.equals(existing.iconUrl(), ext.getIconUrl());

            if (changed) {
                syncRepository.update(existing.id(), ext);
                updated++;
            }
        }

        for (ElementRefView current : currentByCode.values()) {
            if (!externalCodes.contains(current.externalCode())) {
                syncRepository.delete(current.id());
                deleted++;
            }
        }

        return new ElementSyncResult(new PalworldSyncReport(created, updated, deleted), idByExternalCode, idByName);
    }
}
