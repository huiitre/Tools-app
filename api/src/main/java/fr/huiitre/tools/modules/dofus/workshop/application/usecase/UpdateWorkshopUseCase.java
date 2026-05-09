package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.UpdateWorkshopRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopTagDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

@Service
@Transactional
public class UpdateWorkshopUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final WorkshopRepository workshopRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public UpdateWorkshopUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopRepository workshopRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
    }

    public WorkshopDto execute(Long workshopId, UpdateWorkshopRequest request) {

        Long userId = authenticatedUserProvider.getUserId();

        Workshop current = workshopRepository.findByIdAndUserId(userId, workshopId)
                .orElseThrow(() -> new WorkshopNotFoundException());

        boolean exists = workshopRepository.existsByUserIdAndName(
                userId,
                request.name());

        if (exists && !current.getName().equalsIgnoreCase(request.name())) {
            throw new IllegalArgumentException("Un atelier avec ce nom existe déjà.");
        }

        boolean activeValue = request.active() != null ? request.active() : current.isActive();
        boolean pinnedValue = request.pinned() != null ? request.pinned() : current.isPinned();

        current.update(
            request.name(),
            activeValue,
            pinnedValue
        );

        workshopRepository.update(userId, current);

        List<WorkshopTag> tags = workshopRepository.findAllTagsByUserIdAndWorkshopId(userId, current.getId());
        List<WorkshopTagDto> tagsDto = tags.stream()
                .map(tag -> new WorkshopTagDto(tag.getId(), tag.getName(), tag.getColor()))
                .toList();

        List<WorkshopLinkDto> linksDto = current.getLinks().stream()
                .map(link -> new WorkshopLinkDto(link.getId(), link.getSource(), link.getUrl(), link.getLabel()))
                .toList();

        return new WorkshopDto(
            current.getId(),
            current.getName(),
            current.isActive(),
            tagsDto,
            linksDto,
            current.isPinned()
        );
    }
}