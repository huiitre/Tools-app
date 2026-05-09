package fr.huiitre.tools.modules.dofus.almanax.application.usecase;

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxRepository;
import fr.huiitre.tools.modules.dofus.almanax.application.view.AlmanaxDto;
import fr.huiitre.tools.modules.dofus.almanax.domain.Almanax;
import fr.huiitre.tools.modules.dofus.almanax.domain.DatePattern;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.dto.FarmZoneDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemImageDto;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.application.service.ItemEnrichmentService;
import fr.huiitre.tools.modules.dofus.sync.application.views.AssetImageUrlBuilder;

@Service
@Transactional(readOnly = true)
public class GetAlmanaxCalendarUseCase implements SecuredUseCase {

    private final AlmanaxRepository almanaxRepository;
    private final GameVersionRepository gameVersionRepository;
    private final ItemRepository itemRepository;

    private final ItemEnrichmentService itemEnrichmentService;

    private final AssetImageUrlBuilder assetImageUrlBuilder;

    private static final Logger logger = LoggerFactory.getLogger(GetAlmanaxCalendarUseCase.class);

    private final AuthenticatedUserProvider authenticatedUserProvider;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetAlmanaxCalendarUseCase(
            AlmanaxRepository almanaxRepository,
            AuthenticatedUserProvider authenticatedUserProvider,
            GameVersionRepository gameVersionRepository,
            ItemRepository itemRepository,
            AssetImageUrlBuilder assetImageUrlBuilder,
            ItemEnrichmentService itemEnrichmentService) {
        this.almanaxRepository = almanaxRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.gameVersionRepository = gameVersionRepository;
        this.itemRepository = itemRepository;
        this.assetImageUrlBuilder = assetImageUrlBuilder;
        this.itemEnrichmentService = itemEnrichmentService;
    }

    public List<AlmanaxDto> execute() {

        List<Almanax> almanaxList = almanaxRepository.findAll();
        GameVersionData gameVersion = gameVersionRepository.findByCode("dofus3");

        LocalDate start = LocalDate.now();
        LocalDate end = start.plusYears(2);

        // * on calcule les associations date → almanax en mémoire */
        Map<LocalDate, Almanax> dateToAlmanax = new HashMap<>();
        List<LocalDate> missingDates = new ArrayList<>();

        for (LocalDate date = start; !date.isAfter(end); date = date.plusDays(1)) {
            Almanax best = findBestAlmanaxForDate(date, almanaxList);
            if (best != null) {
                dateToAlmanax.put(date, best);
            } else {
                missingDates.add(date);
            }
        }

        // * on récupère tous les itemids uniques
        Set<Long> itemIds = dateToAlmanax.values().stream()
                .map(Almanax::getItemId)
                .collect(Collectors.toSet());

        // * on récupère tous les items */
        Map<Long, ItemDto> itemsById = itemRepository
                .findByGameVersionIdAndItemIds(gameVersion.getId(), itemIds);

        // * on récupère toutes les zones par itemIds */
        Map<Long, List<FarmZoneDto>> farmZonesByItemId = itemEnrichmentService.loadFarmZones(new ArrayList<>(itemIds), gameVersion.getCode());

        // * on récupère toutes les images */
        Map<Long, List<ItemImageDto>> imagesByItemId = itemEnrichmentService.loadItemImages(new ArrayList<>(itemIds),gameVersion.getCode());

        // * on assemble les DTOs */
        List<AlmanaxDto> result = new ArrayList<>();
        for (LocalDate date = start; !date.isAfter(end); date = date.plusDays(1)) {
            Almanax almanax = dateToAlmanax.get(date);
            if (almanax != null) {
                ItemDto item = itemsById.get(almanax.getItemId());
                List<ItemImageDto> images = imagesByItemId.getOrDefault(almanax.getItemId(), List.of());
                List<FarmZoneDto> farmZones = farmZonesByItemId.getOrDefault(almanax.getItemId(), List.of());

                ItemDto itemWithImages = new ItemDto(
                    item.getId(),
                    item.getName(),
                    item.getDescription(),
                    item.isHasRecipe(),
                    item.getAssetId(),
                    item.getGameVersionId(),
                    item.getLevel(),
                    item.getType(),
                    images,
                    null,
                    null,
                    farmZones);

                result.add(new AlmanaxDto(
                        almanax.getId(),
                        almanax.getName(),
                        almanax.getDescription(),
                        date,
                        itemWithImages,
                        almanax.getItemQuantity()));
            }
        }

        if (!missingDates.isEmpty()) {
            logger.debug("Almanax missing for {} days: {}", missingDates.size(), missingDates);
        }

        return result;
    }

    private Almanax findBestAlmanaxForDate(LocalDate date, List<Almanax> almanaxList) {
        Almanax bestAlmanax = null;
        int bestScore = 0;
        int bestPatternCount = Integer.MAX_VALUE;

        for (Almanax almanax : almanaxList) {
            int score = almanax.getDates().stream()
                    .mapToInt(raw -> new DatePattern(raw).score(date))
                    .max()
                    .orElse(0);

            if (score == 0)
                continue;

            int patternCount = almanax.getDates().size();
            if (score > bestScore || (score == bestScore && patternCount < bestPatternCount)) {
                bestScore = score;
                bestPatternCount = patternCount;
                bestAlmanax = almanax;
            }
        }

        return bestAlmanax;
    }
}