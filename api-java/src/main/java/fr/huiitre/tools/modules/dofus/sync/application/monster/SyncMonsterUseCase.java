package fr.huiitre.tools.modules.dofus.sync.application.monster;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.domain.Item;
import fr.huiitre.tools.modules.dofus.monster.application.ports.MonsterRepository;
import fr.huiitre.tools.modules.dofus.monster.domain.Monster;
import fr.huiitre.tools.modules.dofus.subarea.application.ports.SubareaRepository;
import fr.huiitre.tools.modules.dofus.subarea.domain.Subarea;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;

@Service
@Transactional
public class SyncMonsterUseCase implements SecuredUseCase {

    private final MonsterDataProvider monsterDataProvider;
    private final MonsterRepository monsterRepository;
    private final SubareaRepository subareaRepository;
    private final ItemRepository itemRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public SyncMonsterUseCase(
        MonsterDataProvider monsterDataProvider,
        MonsterRepository monsterRepository,
        SubareaRepository subareaRepository,
        ItemRepository itemRepository
    ) {
        this.monsterDataProvider = monsterDataProvider;
        this.monsterRepository = monsterRepository;
        this.subareaRepository = subareaRepository;
        this.itemRepository = itemRepository;
    }

    public SyncReport execute(GameVersionData gameVersion) {
        
        List<MonsterSyncData> external = monsterDataProvider.fetchAll();

        List<Monster> current = monsterRepository.findAllByGameVersionId(gameVersion.getId());

        Map<Long, Monster> currentByAssetId = current.stream().collect(Collectors.toMap(
            Monster::getAssetId,
            it -> it
        ));

        Map<Long, Subarea> subareasByAssetId = subareaRepository.findAllByGameVersionId(gameVersion.getId()).stream()
            .collect(Collectors.toMap(
                Subarea::getAssetId,
                it -> it
            ));

        Map<Long, Item> itemsByAssetId = itemRepository.findAllByGameVersionId(gameVersion.getId()).stream()
            .collect(Collectors.toMap(
                Item::getAssetId,
                it -> it
            ));

        List<String> created = new ArrayList<>();
        List<String> updated = new ArrayList<>();

        for (MonsterSyncData ext : external) {

            Monster existing = currentByAssetId.get(ext.getAssetId());

            // Résoudre asset_ids → internal ids pour subareas
            Set<Long> subareaInternalIds = new HashSet<>();
            for (Long assetId : ext.getSubareaIds()) {
                Subarea subarea = subareasByAssetId.get(assetId);
                if (subarea != null) {
                    subareaInternalIds.add(subarea.getId());
                }
            }

            // Résoudre asset_ids → internal ids pour items (avec dédoublonnage automatique)
            Set<Long> itemInternalIds = new HashSet<>();
            for (Long assetId : ext.getItemIds()) {
                Item item = itemsByAssetId.get(assetId);
                if (item != null) {
                    itemInternalIds.add(item.getId());
                }
            }

            if (existing == null) {
                
                Monster createdItem = Monster.create(
                    ext.getAssetId(),
                    gameVersion.getId(),
                    ext.getName()
                );

                Long id = monsterRepository.insert(createdItem);

                created.add(
                    "assetId=" + ext.getAssetId()
                    + " name=\"" + ext.getName() + "\""
                    + " iconId=" + ext.getIconId()
                );

                monsterRepository.refreshImages(id, ext.getIconId());
                monsterRepository.refreshSubareas(id, subareaInternalIds);
                monsterRepository.refreshDrops(id, itemInternalIds);

                continue;
            }

            boolean nameChanged = !existing.getName().equals(ext.getName());

            if (nameChanged) {

                String oldName = existing.getName();

                existing.update(ext.getName());
                monsterRepository.update(existing);

                updated.add(
                    "assetId=" + ext.getAssetId()
                    + " name=\"" + oldName + "\" -> \"" + ext.getName() + "\""
                );
            }

            monsterRepository.refreshImages(existing.getId(), ext.getIconId());
            monsterRepository.refreshSubareas(existing.getId(), subareaInternalIds);
            monsterRepository.refreshDrops(existing.getId(), itemInternalIds);
        }

        return new SyncReport(
            "monsters",
            "Monsters",
            created,
            updated
        );
    }
}