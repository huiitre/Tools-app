package fr.huiitre.tools.modules.dofus.workshop.application.service;

import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import org.springframework.stereotype.Component;

import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.sync.application.views.AssetImageUrlBuilder;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopBreedDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopJobDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopTagDto;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopBreed;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopJob;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopLink;

//* Seul endroit qui construit un WorkshopDto : tags, liens, classes, métiers et objets.
@Component
public class WorkshopDtoAssembler {

    private final WorkshopRepository workshopRepository;
    private final AssetImageUrlBuilder assetImageUrlBuilder;
    private final WorkshopItemsBuilder workshopItemsBuilder;
    private final GameVersionRepository gameVersionRepository;

    public WorkshopDtoAssembler(
            WorkshopRepository workshopRepository,
            AssetImageUrlBuilder assetImageUrlBuilder,
            WorkshopItemsBuilder workshopItemsBuilder,
            GameVersionRepository gameVersionRepository) {
        this.workshopRepository = workshopRepository;
        this.assetImageUrlBuilder = assetImageUrlBuilder;
        this.workshopItemsBuilder = workshopItemsBuilder;
        this.gameVersionRepository = gameVersionRepository;
    }

    public WorkshopDto toDto(Long userId, Workshop workshop) {
        return toDtos(userId, List.of(workshop), Map.of(workshop.getId(), workshop.getLinks())).get(0);
    }

    public List<WorkshopDto> toDtos(Long userId, List<Workshop> workshops, Map<Long, List<WorkshopLink>> linksByWorkshopId) {
        if (workshops.isEmpty()) {
            return List.of();
        }

        List<Long> ids = workshops.stream().map(Workshop::getId).toList();
        Map<Long, List<WorkshopBreedDto>> breeds = breedsByWorkshopId(ids);
        //* Les ateliers d'une même liste partagent la version du jeu.
        GameVersionData gameVersion = gameVersionRepository.findById(workshopRepository.findGameVersionId(ids.get(0)))
            .orElseThrow();
        Map<Long, List<WorkshopJobDto>> jobs = jobsByWorkshopId(ids);

        return workshops.stream()
            .map(workshop -> new WorkshopDto(
                workshop.getId(),
                workshop.getName(),
                workshop.isActive(),
                workshopRepository.findAllTagsByUserIdAndWorkshopId(userId, workshop.getId()).stream()
                    .map(tag -> new WorkshopTagDto(tag.getId(), tag.getName(), tag.getColor()))
                    .toList(),
                linksByWorkshopId.getOrDefault(workshop.getId(), List.of()).stream()
                    .map(link -> new WorkshopLinkDto(link.getId(), link.getSource(), link.getUrl(), link.getLabel()))
                    .toList(),
                workshop.isPinned(),
                breeds.getOrDefault(workshop.getId(), List.of()),
                jobs.getOrDefault(workshop.getId(), List.of()),
                // ponytail: quelques requêtes par objet et par atelier ; à regrouper si la liste devient lente
                workshopItemsBuilder.build(userId, workshop.getId(), gameVersion, false)))
            .toList();
    }

    public WorkshopBreedDto breedDto(Long breedId, String name, Long headIconId) {
        return new WorkshopBreedDto(breedId, name, iconUrl("class_head", headIconId == null ? null : "Head_" + headIconId));
    }

    public WorkshopJobDto jobDto(Long jobId, String name, Long iconId) {
        return new WorkshopJobDto(jobId, name, iconUrl("job", iconId == null ? null : String.valueOf(iconId)));
    }

    private String iconUrl(String category, String fileBaseName) {
        return fileBaseName == null ? null : assetImageUrlBuilder.buildIcon(category, fileBaseName);
    }

    public List<WorkshopBreedDto> breedsOf(Long workshopId) {
        return breedsByWorkshopId(List.of(workshopId)).getOrDefault(workshopId, List.of());
    }

    public List<WorkshopJobDto> jobsOf(Long workshopId) {
        return jobsByWorkshopId(List.of(workshopId)).getOrDefault(workshopId, List.of());
    }

    private Map<Long, List<WorkshopBreedDto>> breedsByWorkshopId(Collection<Long> workshopIds) {
        return workshopRepository.findBreedsByWorkshopIds(workshopIds).stream()
            .collect(Collectors.groupingBy(
                WorkshopBreed::workshopId,
                Collectors.mapping(b -> breedDto(b.breedId(), b.name(), b.iconId()), Collectors.toList())));
    }

    private Map<Long, List<WorkshopJobDto>> jobsByWorkshopId(Collection<Long> workshopIds) {
        return workshopRepository.findJobsByWorkshopIds(workshopIds).stream()
            .collect(Collectors.groupingBy(
                WorkshopJob::workshopId,
                Collectors.mapping(j -> jobDto(j.jobId(), j.name(), j.iconId()), Collectors.toList())));
    }
}
