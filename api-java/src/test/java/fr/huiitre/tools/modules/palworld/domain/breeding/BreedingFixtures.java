package fr.huiitre.tools.modules.palworld.domain.breeding;

import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

/**
 * Charge les fixtures réelles (extraites des vrais assets Palworld) : un id technique synthétique
 * est attribué par tribe, cohérent entre {@link #loadAllPals()} et {@link #loadRawExceptionEntries()},
 * pour reproduire fidèlement les données de jeu sans dépendre d'une base de données en test.
 */
final class BreedingFixtures {

    record RawExceptionEntry(String parentATribe, String parentAGenderCode, String parentBTribe,
            String parentBGenderCode, String childTribe) {}

    private BreedingFixtures() {}

    private static final ObjectMapper MAPPER = new ObjectMapper();

    static List<BreedingPal> loadAllPals() {
        try (InputStream in = BreedingFixtures.class.getResourceAsStream("/palworld/pals-breeding-fixture.json")) {
            JsonNode root = MAPPER.readTree(in);
            List<BreedingPal> result = new ArrayList<>();
            long syntheticId = 1;
            for (JsonNode pal : root) {
                result.add(new BreedingPal(
                        syntheticId++,
                        pal.path("tribe").asText(null),
                        pal.path("tribe").asText(null),
                        pal.path("combiRank").isNull() ? null : pal.path("combiRank").asInt(),
                        pal.path("combiDuplicatePriority").isNull() ? null : pal.path("combiDuplicatePriority").asInt(),
                        pal.path("ignoreCombi").asBoolean(false)));
            }
            return result;
        } catch (IOException e) {
            throw new IllegalStateException("Failed to load pals-breeding-fixture.json", e);
        }
    }

    static Map<String, Long> tribeToId(List<BreedingPal> pals) {
        Map<String, Long> result = new LinkedHashMap<>();
        for (BreedingPal pal : pals) {
            result.put(pal.tribe(), pal.id());
        }
        return result;
    }

    static List<RawExceptionEntry> loadRawExceptionEntries() {
        try (InputStream in = BreedingFixtures.class.getResourceAsStream("/palworld/breeding.json")) {
            JsonNode root = MAPPER.readTree(in);
            List<RawExceptionEntry> result = new ArrayList<>();
            for (JsonNode entry : root) {
                JsonNode parentA = entry.path("parentA");
                JsonNode parentB = entry.path("parentB");
                result.add(new RawExceptionEntry(
                        parentA.path("tribe").asText(null),
                        parentA.path("gender").asText(null),
                        parentB.path("tribe").asText(null),
                        parentB.path("gender").asText(null),
                        entry.path("child").asText(null)));
            }
            return result;
        } catch (IOException e) {
            throw new IllegalStateException("Failed to load breeding.json fixture", e);
        }
    }

    /** Ne conserve que les lignes dont les 3 espèces (parentA/parentB/child) existent dans le fixture pals. */
    static List<BreedingException> loadValidExceptions(Map<String, Long> tribeToId) {
        List<BreedingException> result = new ArrayList<>();
        for (RawExceptionEntry e : loadRawExceptionEntries()) {
            Long a = tribeToId.get(e.parentATribe());
            Long b = tribeToId.get(e.parentBTribe());
            Long c = tribeToId.get(e.childTribe());
            if (a == null || b == null || c == null) continue;
            result.add(new BreedingException(a, Gender.fromCode(e.parentAGenderCode()), b, Gender.fromCode(e.parentBGenderCode()), c));
        }
        return result;
    }

    static BreedingPal byTribe(List<BreedingPal> pals, String tribe) {
        return pals.stream()
                .filter(p -> p.tribe().equals(tribe))
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException("Unknown fixture tribe: " + tribe));
    }
}
