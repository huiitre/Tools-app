package fr.huiitre.tools.modules.palworld.domain.breeding;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

// Construit l'ensemble des paires (parentA, parentB) -> enfant pour tout le catalogue, sans jamais
// persister de table de toutes les paires (309 espèces -> ~48k paires non-ordonnées). Recalculé à la
// demande à chaque appel de GetBreedingParentsUseCase — assez rapide (cf. BreedingEngineTest) pour ne
// pas justifier de cache.
public final class BreedingIndexBuilder {

    private BreedingIndexBuilder() {}

    public static List<BreedingPairResult> buildAll(List<BreedingPal> allPals, List<BreedingException> exceptions) {
        List<BreedingPairResult> results = new ArrayList<>();
        Set<SpeciesPairKey> exceptionCoveredPairs = new HashSet<>();

        for (BreedingException e : exceptions) {
            results.add(new BreedingPairResult(
                    e.parentAPalId(), e.parentAGender(), e.parentBPalId(), e.parentBGender(),
                    BreedingRule.EXCEPTION, e.childPalId(), null));
            exceptionCoveredPairs.add(SpeciesPairKey.of(e.parentAPalId(), e.parentBPalId()));
        }

        int n = allPals.size();
        for (int i = 0; i < n; i++) {
            BreedingPal a = allPals.get(i);
            if (a.combiRank() == null) continue;

            for (int j = i; j < n; j++) {
                BreedingPal b = allPals.get(j);
                if (b.combiRank() == null) continue;
                if (exceptionCoveredPairs.contains(SpeciesPairKey.of(a.id(), b.id()))) continue;

                // exceptions=List.of() : la paire n'est, par construction, couverte par aucune exception
                // (cf. filtre ci-dessus) -> la branche formule est garantie, pas besoin de rescanner.
                BreedingComputation computation = BreedingEngine.compute(a, null, b, null, List.of(), allPals);
                results.add(new BreedingPairResult(
                        a.id(), null, b.id(), null, BreedingRule.FORMULA, computation.childPalId(), computation.formulaDetails()));
            }
        }

        return results;
    }

    private record SpeciesPairKey(long lo, long hi) {
        static SpeciesPairKey of(long a, long b) {
            return a <= b ? new SpeciesPairKey(a, b) : new SpeciesPairKey(b, a);
        }
    }
}
