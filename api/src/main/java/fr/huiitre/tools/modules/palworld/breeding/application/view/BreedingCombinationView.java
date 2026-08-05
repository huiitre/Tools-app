package fr.huiitre.tools.modules.palworld.breeding.application.view;

import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPairResult;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingRule;
import fr.huiitre.tools.modules.palworld.domain.breeding.Gender;

public record BreedingCombinationView(
        BreedingSpeciesRefView parentA,
        BreedingSpeciesRefView parentB,
        String parentAGender,
        String parentBGender,
        String rule,
        BreedingFormulaDetailsView formula,
        BreedingSpeciesRefView child) {

    public static BreedingCombinationView from(
            BreedingPal parentA, BreedingPal parentB, BreedingPal child, BreedingPairResult pairResult) {
        return new BreedingCombinationView(
                BreedingSpeciesRefView.from(parentA),
                BreedingSpeciesRefView.from(parentB),
                genderCode(pairResult.parentAGender()),
                genderCode(pairResult.parentBGender()),
                pairResult.rule() == BreedingRule.EXCEPTION ? "exception" : "formula",
                BreedingFormulaDetailsView.from(pairResult.formulaDetails()),
                BreedingSpeciesRefView.from(child));
    }

    private static String genderCode(Gender gender) {
        return gender == null ? null : gender.toCode();
    }
}
