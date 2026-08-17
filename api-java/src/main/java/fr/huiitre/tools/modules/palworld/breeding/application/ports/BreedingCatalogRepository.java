package fr.huiitre.tools.modules.palworld.breeding.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingException;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;

public interface BreedingCatalogRepository {

    List<BreedingPal> findAllPals();

    List<BreedingException> findAllExceptions();
}
