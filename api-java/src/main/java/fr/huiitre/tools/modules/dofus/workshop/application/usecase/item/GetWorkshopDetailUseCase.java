package fr.huiitre.tools.modules.dofus.workshop.application.usecase.item;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDetailResponse;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopItemDetailDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopItemsBuilder;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;

@Service
@Transactional
public class GetWorkshopDetailUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;
    private final WorkshopItemsBuilder workshopItemsBuilder;
    private final GameVersionRepository gameVersionRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public GetWorkshopDetailUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopRepository workshopRepository,
            WorkshopItemsBuilder workshopItemsBuilder,
            GameVersionRepository gameVersionRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.workshopItemsBuilder = workshopItemsBuilder;
        this.gameVersionRepository = gameVersionRepository;
    }

    public WorkshopDetailResponse execute(Long workshopId, Long gameVersionId) {

        GameVersionData gameVersion = gameVersionRepository.findById(gameVersionId)
            .orElseThrow(() -> new IllegalArgumentException("Game version not found for id: " + gameVersionId));

        Long userId = authenticatedUserProvider.getUserId();

        Workshop workshopDomain = workshopRepository.findById(workshopId, gameVersion.getId())
            .orElseThrow(WorkshopNotFoundException::new);

        Long ownerUserId = workshopRepository.findOwnerUserId(workshopId);

        boolean isOwner = ownerUserId.equals(userId);

        Long effectiveUserId = isOwner ? userId : ownerUserId;

        List<WorkshopItemDetailDto> itemList = workshopItemsBuilder.build(effectiveUserId, workshopId, gameVersion, true);

        List<WorkshopLinkDto> linksDto = workshopDomain.getLinks().stream()
            .map(link -> new WorkshopLinkDto(link.getId(), link.getSource(), link.getUrl(), link.getLabel()))
            .toList();

        return new WorkshopDetailResponse(itemList, linksDto, isOwner);
    }
}
