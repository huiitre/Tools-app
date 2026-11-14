package fr.huiitre.tools.modules.dofus.pricing.application.usecase;

import java.util.Arrays;
import java.util.Locale;

// MANUAL : prix saisi par l'utilisateur, enregistré à son nom.
// CAPTURE : prix moyen du serveur capturé automatiquement, enregistré au nom du compte de capture.
public enum PriceSource {
    MANUAL,
    CAPTURE;

    public static PriceSource fromParam(String value) {
        if (value == null || value.isBlank()) {
            return MANUAL;
        }
        return Arrays.stream(values())
            .filter(s -> s.name().equals(value.trim().toUpperCase(Locale.ROOT)))
            .findFirst()
            .orElseThrow(() -> new IllegalArgumentException(
                "Source de prix inconnue : " + value + " (valeurs acceptées : manual, capture)"));
    }
}
