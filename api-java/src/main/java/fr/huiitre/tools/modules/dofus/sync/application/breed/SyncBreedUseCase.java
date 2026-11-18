package fr.huiitre.tools.modules.dofus.sync.application.breed;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.function.Function;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.breed.application.ports.BreedRepository;
import fr.huiitre.tools.modules.dofus.breed.domain.Breed;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;

@Service
@Transactional
public class SyncBreedUseCase implements SecuredUseCase {

    private final BreedDataProvider breedDataProvider;
    private final BreedRepository breedRepository;

    public SyncBreedUseCase(
            BreedDataProvider breedDataProvider,
            BreedRepository breedRepository) {
        this.breedDataProvider = breedDataProvider;
        this.breedRepository = breedRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public SyncReport execute(GameVersionData gameVersion) {

        Map<Long, Breed> currentByAssetId = breedRepository.findAllByGameVersionId(gameVersion.getId()).stream()
            .collect(Collectors.toMap(Breed::getAssetId, Function.identity()));

        List<String> created = new ArrayList<>();
        List<String> updated = new ArrayList<>();

        for (BreedSyncData ext : breedDataProvider.fetchAll()) {

            Breed existing = currentByAssetId.get(ext.assetId());

            if (existing == null) {
                Long id = breedRepository.insert(Breed.create(ext.assetId(), gameVersion.getId(), ext.name()));
                breedRepository.refreshImages(id, ext.headIconIdsBySex());

                created.add("assetId=" + ext.assetId() + " name=\"" + ext.name() + "\"");
                continue;
            }

            if (!existing.getName().equals(ext.name())) {
                String oldName = existing.getName();
                existing.update(ext.name());
                breedRepository.update(existing);

                updated.add("assetId=" + ext.assetId() + " name=\"" + oldName + "\" -> \"" + ext.name() + "\"");
            }

            breedRepository.refreshImages(existing.getId(), ext.headIconIdsBySex());
        }

        return new SyncReport("breeds", "Breeds", created, updated);
    }
}
