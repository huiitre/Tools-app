package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopTagDto;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopLink;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

@Service
@Transactional
public class ListWorkshopsUseCase implements SecuredUseCase {

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

    public ListWorkshopsUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
    }

    public List<WorkshopDto> execute(Long gameVersionId) {
        
        Long userId = authenticatedUserProvider.getUserId();

        List<Workshop> workshops = workshopRepository.findAllByUserIdAndGameVersionId(userId, gameVersionId);

        Map<Long, List<WorkshopLink>> linksByWorkshopId = workshopRepository.findAllLinksByUserIdGroupedByWorkshopId(userId);

        List<WorkshopDto> workshopsDto = new ArrayList<>();

        for (Workshop workshop : workshops) {
            List<WorkshopTag> tags = workshopRepository.findAllTagsByUserIdAndWorkshopId(userId, workshop.getId());

            List<WorkshopTagDto> tagsDto =
                tags.stream()
                    .map(tag -> {
                        return new WorkshopTagDto(
                            tag.getId(),
                            tag.getName(),
                            tag.getColor()
                        );
                    })
                    .toList();

            List<WorkshopLinkDto> linksDto = linksByWorkshopId.getOrDefault(workshop.getId(), List.of()).stream()
                .map(link -> new WorkshopLinkDto(link.getId(), link.getSource(), link.getUrl(), link.getLabel()))
                .toList();

            workshopsDto.add(
                new WorkshopDto(
                    workshop.getId(),
                    workshop.getName(),
                    workshop.isActive(),
                    tagsDto,
                    linksDto,
                    workshop.isPinned()
                )
            );
        }

        return workshopsDto;
    }
}