package fr.huiitre.tools.modules.palworld.domain.breeding;

public record BreedingComputation(
        BreedingRule rule,
        Long childPalId,
        FormulaDetails formulaDetails,
        Gender exceptionParentAGender,
        Gender exceptionParentBGender) {

    public static BreedingComputation exception(Long childPalId, Gender parentAGender, Gender parentBGender) {
        return new BreedingComputation(BreedingRule.EXCEPTION, childPalId, null, parentAGender, parentBGender);
    }

    public static BreedingComputation formula(Long childPalId, FormulaDetails formulaDetails) {
        return new BreedingComputation(BreedingRule.FORMULA, childPalId, formulaDetails, null, null);
    }
}
