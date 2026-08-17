package fr.huiitre.tools.modules.palworld.breeding.application.exception;

public class BreedingSpeciesNotFoundException extends IllegalArgumentException {
    public BreedingSpeciesNotFoundException(Long palId) {
        super("L'espèce Palworld demandée est introuvable : " + palId);
    }
}
