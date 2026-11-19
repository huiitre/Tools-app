package fr.huiitre.tools.modules.dofus.breed.application.ports;

import java.util.List;
import java.util.Map;

import fr.huiitre.tools.modules.dofus.breed.domain.Breed;
import fr.huiitre.tools.modules.dofus.breed.domain.BreedSex;

public interface BreedRepository {

    List<Breed> findAllByGameVersionId(Long gameVersionId);

    Long insert(Breed breed);

    void update(Breed breed);

    void refreshImages(Long breedId, Map<BreedSex, Long> iconIdsBySex);

    Map<Long, Map<BreedSex, Long>> findHeadIconIdsByBreedId(Long gameVersionId);
}
