package fr.huiitre.tools.modules.palworld.breeding.application.view;

import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;

public record BreedingSpeciesRefView(Long id, String tribe, String name, Integer combiRank) {

    public static BreedingSpeciesRefView from(BreedingPal pal) {
        return pal == null ? null : new BreedingSpeciesRefView(pal.id(), pal.tribe(), pal.name(), pal.combiRank());
    }
}
