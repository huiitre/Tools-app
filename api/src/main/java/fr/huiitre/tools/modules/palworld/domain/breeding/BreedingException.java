package fr.huiitre.tools.modules.palworld.domain.breeding;

public record BreedingException(
        Long parentAPalId,
        Gender parentAGender,
        Long parentBPalId,
        Gender parentBGender,
        Long childPalId) {}
