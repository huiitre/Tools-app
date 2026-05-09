package fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopTagDto;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopTagRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

@Service
@Transactional
public class ListWorkshopTagsUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final WorkshopTagRepository workshopTagRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public ListWorkshopTagsUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopTagRepository workshopTagRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopTagRepository = workshopTagRepository;
    }

    public List<WorkshopTagDto> execute(Long gameVersionId) {
        Long userId = authenticatedUserProvider.getUserId();

        List<WorkshopTag> tags = workshopTagRepository.findAllByUserIdAndGameVersionId(
                userId,
                gameVersionId);

        return tags.stream()
                .map(tag -> new WorkshopTagDto(
                        tag.getId(),
                        tag.getName(),
                        tag.getColor()))
                .toList();
    }
}