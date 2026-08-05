package fr.huiitre.tools.modules.palworld.breeding.application.view;

import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingComputation;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingRule;
import fr.huiitre.tools.modules.palworld.domain.breeding.Gender;

public record BreedingResultView(
        BreedingSpeciesRefView parentA,
        BreedingSpeciesRefView parentB,
        BreedingSpeciesRefView child,
        String rule,
        BreedingFormulaDetailsView formula,
        BreedingExceptionDetailsView exception) {

    public static BreedingResultView from(BreedingPal parentA, BreedingPal parentB, BreedingPal child, BreedingComputation computation) {
        boolean isException = computation.rule() == BreedingRule.EXCEPTION;
        return new BreedingResultView(
                BreedingSpeciesRefView.from(parentA),
                BreedingSpeciesRefView.from(parentB),
                BreedingSpeciesRefView.from(child),
                isException ? "exception" : "formula",
                isException ? null : BreedingFormulaDetailsView.from(computation.formulaDetails()),
                isException
                        ? new BreedingExceptionDetailsView(
                                genderCode(computation.exceptionParentAGender()), genderCode(computation.exceptionParentBGender()))
                        : null);
    }

    private static String genderCode(Gender gender) {
        return gender == null ? null : gender.toCode();
    }
}
