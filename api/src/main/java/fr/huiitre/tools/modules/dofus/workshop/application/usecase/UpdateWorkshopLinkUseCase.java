package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.UpdateWorkshopLinkRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopLinkNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopLinkMetadataResolver;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopLink;

@Service
@Transactional
public class UpdateWorkshopLinkUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;
    private final WorkshopLinkMetadataResolver metadataResolver;

    public UpdateWorkshopLinkUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopRepository workshopRepository,
            WorkshopLinkMetadataResolver metadataResolver) {
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

    public WorkshopLinkDto execute(Long workshopId, Long linkId, UpdateWorkshopLinkRequest request) {
        Long userId = authenticatedUserProvider.getUserId();

        if (!workshopRepository.existsByIdAndUserId(userId, workshopId)) {
            throw new WorkshopNotFoundException();
        }

        WorkshopLink link = workshopRepository.findLinkByIdAndWorkshopId(userId, workshopId, linkId)
                .orElseThrow(WorkshopLinkNotFoundException::new);

        if (request.url() == null || request.url().isBlank()) {
            throw new IllegalArgumentException("L'URL ne peut pas être vide.");
        }
        if (request.label() == null || request.label().isBlank()) {
            throw new IllegalArgumentException("Le titre ne peut pas être vide.");
        }

        metadataResolver.validate(link.getSource(), request.url().trim());

        workshopRepository.updateLink(userId, workshopId, linkId, request.url().trim(), request.label().trim());

        return new WorkshopLinkDto(linkId, link.getSource(), request.url().trim(), request.label().trim());
    }
}
