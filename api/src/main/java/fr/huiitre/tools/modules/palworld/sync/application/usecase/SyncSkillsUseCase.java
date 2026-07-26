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
import fr.huiitre.tools.modules.palworld.sync.application.SkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.SkillSyncResult;
import fr.huiitre.tools.modules.palworld.sync.application.ports.SkillDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.SkillSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.SkillRefView;

@Service
@Transactional
public class SyncSkillsUseCase implements SecuredUseCase {

    private final SkillDataProvider dataProvider;
    private final SkillSyncRepository syncRepository;

    public SyncSkillsUseCase(SkillDataProvider dataProvider, SkillSyncRepository syncRepository) {
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

    public SkillSyncResult execute(Map<String, Long> elementIdByExternalCode) {
        List<SkillSyncData> external = dataProvider.fetchAll();

        Map<String, SkillRefView> currentBySlug = syncRepository.findAll().stream()
                .collect(Collectors.toMap(SkillRefView::slug, it -> it));

        Set<String> externalSlugs = external.stream()
                .map(SkillSyncData::getSlug)
                .collect(Collectors.toSet());

        Map<String, Long> idBySlug = new HashMap<>();
        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (SkillSyncData ext : external) {
            Long elementId = elementIdByExternalCode.get(ext.getElementExternalCode());
            SkillRefView existing = currentBySlug.get(ext.getSlug());

            if (existing == null) {
                Long newId = syncRepository.save(ext, elementId);
                idBySlug.put(ext.getSlug(), newId);
                syncRepository.upsertSource(newId, ext.getSlug(), ext.getSourceUrl(), ext.getRawPayloadJson(), ext.getFetchedAt());
                created++;
                continue;
            }

            idBySlug.put(ext.getSlug(), existing.id());

            boolean changed = !Objects.equals(existing.category(), ext.getCategory())
                    || !Objects.equals(existing.name(), ext.getName())
                    || !Objects.equals(existing.iconUrl(), ext.getIconUrl())
                    || !Objects.equals(existing.elementId(), elementId)
                    || !Objects.equals(existing.cooldown(), ext.getCooldown())
                    || !Objects.equals(existing.power(), ext.getPower())
                    || !Objects.equals(existing.statusEffect(), ext.getStatusEffect())
                    || !Objects.equals(existing.description(), ext.getDescription());

            if (changed) {
                syncRepository.update(existing.id(), ext, elementId);
                updated++;
            }

            syncRepository.upsertSource(existing.id(), ext.getSlug(), ext.getSourceUrl(), ext.getRawPayloadJson(), ext.getFetchedAt());
        }

        for (SkillRefView current : currentBySlug.values()) {
            if (!externalSlugs.contains(current.slug())) {
                syncRepository.delete(current.id());
                deleted++;
            }
        }

        return new SkillSyncResult(new PalworldSyncReport(created, updated, deleted), idBySlug);
    }
}
