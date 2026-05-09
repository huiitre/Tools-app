package fr.huiitre.tools.modules.dofus.sync.application.area;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.area.application.ports.AreaRepository;
import fr.huiitre.tools.modules.dofus.area.domain.Area;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;

@Service
@Transactional
public class SyncAreaUseCase implements SecuredUseCase {

    private final AreaDataProvider areaDataProvider;

    private final AreaRepository areaRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public SyncAreaUseCase(
            AreaDataProvider areaDataProvider,
            AreaRepository areaRepository) {
        this.areaDataProvider = areaDataProvider;
        this.areaRepository = areaRepository;
    }

    public SyncReport execute(GameVersionData gameVersion) {

        List<AreaSyncData> external = areaDataProvider.fetchAll();

        List<Area> current = areaRepository.findAllByGameVersionId(gameVersion.getId());

        Map<Long, Area> currentByAssetId = current.stream().collect(Collectors.toMap(
                Area::getAssetId, it -> it));

        List<String> created = new ArrayList<>();
        List<String> updated = new ArrayList<>();

        for (AreaSyncData ext : external) {

            Area existing = currentByAssetId.get(ext.getAssetId());

            if (existing == null) {

                Area createdItem = Area.create(
                        ext.getAssetId(),
                        gameVersion.getId(),
                        ext.getName());

                areaRepository.insert(createdItem);

                created.add(
                        "assetId=" + ext.getAssetId()
                                + " name=\"" + ext.getName() + "\"");

                continue;
            }

            boolean nameChanged = !existing.getName().equals(ext.getName());

            if (nameChanged) {

                String oldName = existing.getName();

                existing.update(
                        ext.getName());

                areaRepository.update(existing);

                updated.add(
                        "assetId=" + ext.getAssetId()
                                + " name=\"" + oldName + "\"->\"" + ext.getName() + "\"");
            }
        }

        return new SyncReport(
                "areas",
                "Areas",
                created,
                updated);
    }
}