package fr.huiitre.tools.modules.dofus.workshop.domain;

import static org.junit.jupiter.api.Assertions.assertEquals;

import org.junit.jupiter.api.Test;

import fr.huiitre.tools.modules.dofus.breed.domain.BreedSex;

class WorkshopBreedSexTest {

    @Test
    void maleBreeds() {
        // Enutrof, Sram, Iop, Sacrieur, Pandawa, Zobal, Ouginak, Forgelance
        for (long assetId : new long[] { 3, 4, 8, 11, 12, 14, 18, 20 }) {
            assertEquals(BreedSex.MALE, WorkshopBreedSex.of(assetId), "asset_id " + assetId);
        }
    }

    @Test
    void otherBreedsAreFemale() {
        // Féca, Osamodas, Xélor, Ecaflip, Eniripsa, Crâ, Sadida, Roublard, Steamer, Eliotrope, Huppermage
        for (long assetId : new long[] { 1, 2, 5, 6, 7, 9, 10, 13, 15, 16, 17 }) {
            assertEquals(BreedSex.FEMALE, WorkshopBreedSex.of(assetId), "asset_id " + assetId);
        }
    }
}
