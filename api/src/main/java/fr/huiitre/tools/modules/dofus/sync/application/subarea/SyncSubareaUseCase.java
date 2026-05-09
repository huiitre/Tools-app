package fr.huiitre.tools.modules.dofus.sync.application.subarea;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.area.application.ports.AreaRepository;
import fr.huiitre.tools.modules.dofus.area.domain.Area;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.subarea.application.ports.SubareaRepository;
import fr.huiitre.tools.modules.dofus.subarea.domain.Subarea;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;

@Service
@Transactional
public class SyncSubareaUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final SubAreaDataProvider subAreaDataProvider;
    private final SubareaRepository subareaRepository;
    private final AreaRepository areaRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public SyncSubareaUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            SubAreaDataProvider subAreaDataProvider,
            SubareaRepository subareaRepository,
            AreaRepository areaRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.subAreaDataProvider = subAreaDataProvider;
        this.subareaRepository = subareaRepository;
        this.areaRepository = areaRepository;
    }

    public SyncReport execute(GameVersionData gameVersion) {

        List<SubareaSyncData> external = subAreaDataProvider.fetchAll();

        List<Subarea> current = subareaRepository.findAllByGameVersionId(gameVersion.getId());

        Map<Long, Subarea> currentByAssetId = current.stream().collect(Collectors.toMap(
                Subarea::getAssetId,
                it -> it));

        Map<Long, Area> areaByAssetId = areaRepository.findAllByGameVersionId(gameVersion.getId()).stream().collect(Collectors.toMap(
                Area::getAssetId,
                it -> it));

        List<String> created = new java.util.ArrayList<>();
        List<String> updated = new java.util.ArrayList<>();

        for (SubareaSyncData ext : external) {

            Subarea existing = currentByAssetId.get(ext.getAssetId());

            Area existingArea = areaByAssetId.get(ext.getAreaId());

            if (existingArea == null) {
                continue;
            }

            if (existing == null) {
                Subarea createdItem = Subarea.create(
                        ext.getAssetId(),
                        gameVersion.getId(),
                        existingArea.getId(),
                        ext.getName());
                subareaRepository.insert(createdItem);
                created.add(
                        "assetId=" + ext.getAssetId()
                                + " name=\"" + ext.getName() + "\""
                                + " areaId=" + existingArea.getId());
                continue;
            }

            boolean nameChanged = !existing.getName().equals(ext.getName());

            if (nameChanged) {

                String oldName = existing.getName();

                existing.update(
                        existingArea.getId(),
                        ext.getName());

                subareaRepository.update(existing);

                updated.add(
                        "assetId=" + ext.getAssetId()
                                + (nameChanged ? " name=\"" + oldName + "\"->\"" + ext.getName() + "\"" : ""));
            }
        }

        return new SyncReport(
                "subareas",
                "Subareas",
                created,
                updated);
    }
}