package fr.huiitre.tools.modules.palworld.domain.breeding;

public record BreedingPal(
        Long id,
        String tribe,
        String name,
        Integer combiRank,
        Integer combiDuplicatePriority,
        boolean ignoreCombi) {}
