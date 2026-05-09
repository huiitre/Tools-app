package fr.huiitre.tools.modules.dofus.itemtype.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.itemtype.application.ports.ItemTypeRepository;
import fr.huiitre.tools.modules.dofus.itemtype.application.view.ItemTypeDto;

@Service
@Transactional(readOnly = true)
public class ListItemTypeUseCase implements SecuredUseCase {

    private final ItemTypeRepository itemTypeRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ListItemTypeUseCase(ItemTypeRepository itemTypeRepository) {
        this.itemTypeRepository = itemTypeRepository;
    }

    public List<ItemTypeDto> execute(Long gameVersionId) {
        return this.itemTypeRepository.findAllByGameVersionId(gameVersionId)
                .stream()
                .map(itemType -> new ItemTypeDto(
                        itemType.getId(),
                        itemType.getAssetId(),
                        itemType.getGameVersionId(),
                        itemType.getName()))
                .toList();
    }
}