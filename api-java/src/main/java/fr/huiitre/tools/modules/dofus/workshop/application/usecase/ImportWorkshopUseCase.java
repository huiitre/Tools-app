package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.AddWorkshopLinkRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.CreateWorkshopRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.ImportWorkshopRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.UpdateWorkshopLinkRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopItemDetailDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopTagRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopDtoAssembler;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.breed.AddBreedsToWorkshopUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.AddItemsToWorkshopUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.UpdateWorkshopItemQuantityUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.job.AddJobsToWorkshopUseCase;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItem;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

//* Copie l'atelier d'un autre utilisateur dans les siens : objets d'origine et leur multiplicateur, repartis de
//* zéro (ingrédients directs, rien d'obtenu, aucun sous-craft), avec les classes, métiers, tags et liens choisis.
//* Tout passe par les use cases existants, dans une seule transaction : une étape refusée n'importe rien.
@Service
@Transactional
public class ImportWorkshopUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;
    private final WorkshopTagRepository workshopTagRepository;
    private final WorkshopDtoAssembler workshopDtoAssembler;
    private final CreateWorkshopUseCase createWorkshopUseCase;
    private final AddItemsToWorkshopUseCase addItemsToWorkshopUseCase;
    private final UpdateWorkshopItemQuantityUseCase updateWorkshopItemQuantityUseCase;
    private final AddBreedsToWorkshopUseCase addBreedsToWorkshopUseCase;
    private final AddJobsToWorkshopUseCase addJobsToWorkshopUseCase;
    private final AddTagsToWorkshopUseCase addTagsToWorkshopUseCase;
    private final AddWorkshopLinkUseCase addWorkshopLinkUseCase;
    private final UpdateWorkshopLinkUseCase updateWorkshopLinkUseCase;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public ImportWorkshopUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository,
        WorkshopTagRepository workshopTagRepository,
        WorkshopDtoAssembler workshopDtoAssembler,
        CreateWorkshopUseCase createWorkshopUseCase,
        AddItemsToWorkshopUseCase addItemsToWorkshopUseCase,
        UpdateWorkshopItemQuantityUseCase updateWorkshopItemQuantityUseCase,
        AddBreedsToWorkshopUseCase addBreedsToWorkshopUseCase,
        AddJobsToWorkshopUseCase addJobsToWorkshopUseCase,
        AddTagsToWorkshopUseCase addTagsToWorkshopUseCase,
        AddWorkshopLinkUseCase addWorkshopLinkUseCase,
        UpdateWorkshopLinkUseCase updateWorkshopLinkUseCase
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.workshopTagRepository = workshopTagRepository;
        this.workshopDtoAssembler = workshopDtoAssembler;
        this.createWorkshopUseCase = createWorkshopUseCase;
        this.addItemsToWorkshopUseCase = addItemsToWorkshopUseCase;
        this.updateWorkshopItemQuantityUseCase = updateWorkshopItemQuantityUseCase;
        this.addBreedsToWorkshopUseCase = addBreedsToWorkshopUseCase;
        this.addJobsToWorkshopUseCase = addJobsToWorkshopUseCase;
        this.addTagsToWorkshopUseCase = addTagsToWorkshopUseCase;
        this.addWorkshopLinkUseCase = addWorkshopLinkUseCase;
        this.updateWorkshopLinkUseCase = updateWorkshopLinkUseCase;
    }

    public WorkshopDto execute(Long sourceWorkshopId, Long gameVersionId, ImportWorkshopRequest request) {
        Long userId = authenticatedUserProvider.getUserId();

        //* Même accès que le détail : tout atelier de la version du jeu est lisible, donc importable.
        workshopRepository.findById(sourceWorkshopId, gameVersionId)
            .orElseThrow(WorkshopNotFoundException::new);

        List<WorkshopItem> sourceItems = workshopRepository.findAllItemsByUserIdAndWorkshopId(
            workshopRepository.findOwnerUserId(sourceWorkshopId), sourceWorkshopId);

        //* Refuse le nom s'il est déjà pris par un de mes ateliers de cette version.
        Long workshopId = createWorkshopUseCase.execute(gameVersionId, new CreateWorkshopRequest(request.name())).getId();

        // Objets d'origine, ajoutés à 1 puis remis à leur multiplicateur
        Map<Long, Long> quantityByItemId = sourceItems.stream()
            .collect(Collectors.toMap(WorkshopItem::getItemId, WorkshopItem::getQuantity, (a, b) -> a));

        List<WorkshopItemDetailDto> created = addItemsToWorkshopUseCase.execute(
            workshopId, gameVersionId, List.copyOf(quantityByItemId.keySet()));

        for (WorkshopItemDetailDto item : created) {
            Long quantity = quantityByItemId.get(item.getItem().getId());
            if (quantity != null && quantity > 1) {
                updateWorkshopItemQuantityUseCase.execute(workshopId, item.getId(), quantity);
            }
        }

        if (!orEmpty(request.breedIds()).isEmpty()) {
            addBreedsToWorkshopUseCase.execute(workshopId, gameVersionId, request.breedIds());
        }

        if (!orEmpty(request.jobIds()).isEmpty()) {
            addJobsToWorkshopUseCase.execute(workshopId, gameVersionId, request.jobIds());
        }

        //* Seulement mes propres tags de cette version : jamais ceux d'un autre utilisateur.
        Set<Long> myTagIds = workshopTagRepository.findAllByUserIdAndGameVersionId(userId, gameVersionId).stream()
            .map(WorkshopTag::getId)
            .collect(Collectors.toSet());
        List<Long> tagIds = orEmpty(request.tagIds()).stream().filter(myTagIds::contains).toList();
        if (!tagIds.isEmpty()) {
            addTagsToWorkshopUseCase.execute(workshopId, tagIds);
        }

        for (ImportWorkshopRequest.Link link : orEmpty(request.links())) {
            WorkshopLinkDto added = addWorkshopLinkUseCase.execute(workshopId, new AddWorkshopLinkRequest(link.source(), link.url()));
            if (link.label() != null && !link.label().isBlank() && !link.label().equals(added.label())) {
                updateWorkshopLinkUseCase.execute(workshopId, added.id(), new UpdateWorkshopLinkRequest(added.url(), link.label()));
            }
        }

        return workshopDtoAssembler.toDto(userId, workshopRepository.findByIdAndUserId(userId, workshopId).orElseThrow());
    }

    private static <T> List<T> orEmpty(List<T> list) {
        return list == null ? List.of() : list;
    }
}
