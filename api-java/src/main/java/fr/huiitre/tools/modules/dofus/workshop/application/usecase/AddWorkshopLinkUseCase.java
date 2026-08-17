package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.AddWorkshopLinkRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopLinkMetadataResolver;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopLink;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Optional;

@Service
@Transactional
public class AddWorkshopLinkUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;
    private final WorkshopLinkMetadataResolver metadataResolver;

    public AddWorkshopLinkUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopRepository workshopRepository,
            WorkshopLinkMetadataResolver metadataResolver
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.metadataResolver = metadataResolver;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public WorkshopLinkDto execute(Long workshopId, AddWorkshopLinkRequest request) {
        Long userId = authenticatedUserProvider.getUserId();

        if (!workshopRepository.existsByIdAndUserId(userId, workshopId)) {
            throw new WorkshopNotFoundException();
        }

        if (workshopRepository.findAllLinksByUserIdAndWorkshopId(userId, workshopId).size() >= 3) {
            throw new IllegalArgumentException("Un atelier ne peut pas avoir plus de 3 liens.");
        }

        String label = metadataResolver.validateAndResolveLabel(request.source(), request.url());
        WorkshopLink link = WorkshopLink.create(request.source(), request.url(), label);

        Long linkId = workshopRepository.addLink(userId, workshopId, link);

        return new WorkshopLinkDto(linkId, link.getSource(), link.getUrl(), link.getLabel());
    }
}
