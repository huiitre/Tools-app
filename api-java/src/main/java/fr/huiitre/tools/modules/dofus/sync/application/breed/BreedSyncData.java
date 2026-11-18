package fr.huiitre.tools.modules.dofus.sync.application.breed;

import java.util.Map;

import fr.huiitre.tools.modules.dofus.breed.domain.BreedSex;

public record BreedSyncData(
        Long assetId,
        String name,
        Map<BreedSex, Long> headIconIdsBySex) {
}
