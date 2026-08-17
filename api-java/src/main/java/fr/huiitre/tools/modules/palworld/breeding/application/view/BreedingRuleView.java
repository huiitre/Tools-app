package fr.huiitre.tools.modules.palworld.breeding.application.view;

public record BreedingRuleView(
        Long parentAPalId,
        String parentAGender,
        Long parentBPalId,
        String parentBGender,
        Long childPalId) {}
