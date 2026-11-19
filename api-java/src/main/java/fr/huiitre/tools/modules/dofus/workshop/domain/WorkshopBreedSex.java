package fr.huiitre.tools.modules.dofus.workshop.domain;

import java.util.Set;

import fr.huiitre.tools.modules.dofus.breed.domain.BreedSex;

//* Tête affichée pour une classe sur un atelier : règle propre à l'atelier, choisie par l'administrateur.
public final class WorkshopBreedSex {

    //* asset_id Dofus 3 : Enutrof, Sram, Iop, Sacrieur, Pandawa, Zobal, Ouginak, Forgelance.
    private static final Set<Long> MALE_BREED_ASSET_IDS = Set.of(3L, 4L, 8L, 11L, 12L, 14L, 18L, 20L);

    private WorkshopBreedSex() {
    }

    public static BreedSex of(long breedAssetId) {
        return MALE_BREED_ASSET_IDS.contains(breedAssetId) ? BreedSex.MALE : BreedSex.FEMALE;
    }
}
