package fr.huiitre.tools.modules.dofus.item.application.service;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.dofus.item.application.dto.FarmZoneDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemImageDto;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.monster.application.dto.MonsterDto;
import fr.huiitre.tools.modules.dofus.monster.application.dto.MonsterImageDto;
import fr.huiitre.tools.modules.dofus.monster.application.ports.MonsterRepository;
import fr.huiitre.tools.modules.dofus.sync.application.views.AssetImageUrlBuilder;
import fr.huiitre.tools.modules.dofus.sync.application.views.AssetResolution;

@Service
public class ItemEnrichmentService {

    private final ItemRepository itemRepository;
    private final MonsterRepository monsterRepository;
    private final AssetImageUrlBuilder assetImageUrlBuilder;

    public ItemEnrichmentService(
        ItemRepository itemRepository,
        MonsterRepository monsterRepository,
        AssetImageUrlBuilder assetImageUrlBuilder
    ) {
        this.itemRepository = itemRepository;
        this.monsterRepository = monsterRepository;
        this.assetImageUrlBuilder = assetImageUrlBuilder;
    }

    public Map<Long, List<ItemImageDto>> loadItemImages(List<Long> itemIds, String gameVersionCode) {
        Map<Long, List<ItemImageDto>> imagesByItemId = new HashMap<>();
        
        for (Long itemId : itemIds) {
            List<ItemImageDto> images = itemRepository.findImageByItemId(itemId);
            buildItemImageUrls(images, gameVersionCode);
            imagesByItemId.put(itemId, images);
        }
        
        return imagesByItemId;
    }

    public Map<Long, List<MonsterImageDto>> loadMonsterImages(Set<Long> monsterIds, String gameVersionCode) {
        List<MonsterImageDto> allImages = monsterRepository.findImageByMonsterIds(monsterIds);
        buildMonsterImageUrls(allImages, gameVersionCode);
        
        return allImages.stream()
            .collect(Collectors.groupingBy(MonsterImageDto::getMonsterId));
    }

    public Map<Long, List<FarmZoneDto>> loadFarmZones(List<Long> itemIds, String gameVersionCode) {
        Map<Long, List<FarmZoneDto>> farmZonesByItemId = itemRepository.findFarmZonesByItemIds(itemIds);

        Set<Long> allMonsterIds = farmZonesByItemId.values().stream()
            .flatMap(List::stream)
            .flatMap(zone -> zone.getMonsters().stream())
            .map(MonsterDto::getId)
            .collect(Collectors.toSet());

        if (allMonsterIds.isEmpty()) {
            return farmZonesByItemId;
        }

        Map<Long, List<MonsterImageDto>> imagesByMonsterId = loadMonsterImages(allMonsterIds, gameVersionCode);

        return enrichFarmZones(farmZonesByItemId, imagesByMonsterId);
    }

    private Map<Long, List<FarmZoneDto>> enrichFarmZones(
        Map<Long, List<FarmZoneDto>> farmZonesByItemId,
        Map<Long, List<MonsterImageDto>> imagesByMonsterId
    ) {
        Map<Long, List<FarmZoneDto>> enriched = new HashMap<>();

        for (Map.Entry<Long, List<FarmZoneDto>> entry : farmZonesByItemId.entrySet()) {
            Long itemId = entry.getKey();
            List<FarmZoneDto> zones = entry.getValue();
            List<FarmZoneDto> enrichedZones = new ArrayList<>();

            for (FarmZoneDto zone : zones) {
                List<MonsterDto> enrichedMonsters = new ArrayList<>();

                for (MonsterDto monster : zone.getMonsters()) {
                    List<MonsterImageDto> monsterImages = imagesByMonsterId.getOrDefault(
                        monster.getId(),
                        List.of()
                    );

                    MonsterDto monsterWithImages = new MonsterDto(
                        monster.getId(),
                        monster.getName(),
                        monsterImages
                    );

                    enrichedMonsters.add(monsterWithImages);
                }

                FarmZoneDto enrichedZone = new FarmZoneDto(
                    zone.getArea(),
                    zone.getSubarea(),
                    enrichedMonsters,
                    zone.isPrimary()
                );

                enrichedZones.add(enrichedZone);
            }

            enriched.put(itemId, enrichedZones);
        }

        return enriched;
    }

    private void buildItemImageUrls(List<ItemImageDto> images, String gameVersionCode) {
        for (ItemImageDto image : images) {
            String url = assetImageUrlBuilder.build(
                "item",
                image.getIconId(),
                AssetResolution.fromDb(image.getResolution()),
                gameVersionCode);
            image.setUrl(url);
        }
    }

    private void buildMonsterImageUrls(List<MonsterImageDto> images, String gameVersionCode) {
        for (MonsterImageDto image : images) {
            String url = assetImageUrlBuilder.build(
                "monster",
                image.getIconId(),
                AssetResolution.fromDb(image.getResolution()),
                gameVersionCode
            );
            image.setUrl(url);
        }
    }
}