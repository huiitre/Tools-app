package fr.huiitre.tools.modules.palworld.breeding.application.view;

import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPairResult;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingRule;
import fr.huiitre.tools.modules.palworld.domain.breeding.Gender;

public record BreedingParentPairView(
        BreedingSpeciesRefView parentA,
        BreedingSpeciesRefView parentB,
        String parentAGender,
        String parentBGender,
        String rule,
        BreedingFormulaDetailsView formula) {

    public static BreedingParentPairView from(BreedingPal parentA, BreedingPal parentB, BreedingPairResult pairResult) {
        return new BreedingParentPairView(
                BreedingSpeciesRefView.from(parentA),
                BreedingSpeciesRefView.from(parentB),
                genderCode(pairResult.parentAGender()),
                genderCode(pairResult.parentBGender()),
                pairResult.rule() == BreedingRule.EXCEPTION ? "exception" : "formula",
                BreedingFormulaDetailsView.from(pairResult.formulaDetails()));
    }

    private static String genderCode(Gender gender) {
        return gender == null ? null : gender.toCode();
    }
}
