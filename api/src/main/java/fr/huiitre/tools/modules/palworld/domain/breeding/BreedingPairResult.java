package fr.huiitre.tools.modules.palworld.domain.breeding;

public record BreedingPairResult(
        Long parentAPalId,
        Gender parentAGender,
        Long parentBPalId,
        Gender parentBGender,
        BreedingRule rule,
        Long childPalId,
        FormulaDetails formulaDetails) {}
