package fr.huiitre.tools.modules.palworld.breeding.application.view;

public record BreedingPathStepView(
        BreedingPathNodeView parentA,
        BreedingPathNodeView parentB,
        String parentAGender,
        String parentBGender,
        String rule) {}
