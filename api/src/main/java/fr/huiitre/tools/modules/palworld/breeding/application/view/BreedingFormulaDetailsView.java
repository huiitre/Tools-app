package fr.huiitre.tools.modules.palworld.breeding.application.view;

import fr.huiitre.tools.modules.palworld.domain.breeding.FormulaDetails;

public record BreedingFormulaDetailsView(int parentARank, int parentBRank, int targetRank, int distance) {

    public static BreedingFormulaDetailsView from(FormulaDetails details) {
        return details == null ? null
                : new BreedingFormulaDetailsView(details.parentARank(), details.parentBRank(), details.targetRank(), details.distance());
    }
}
