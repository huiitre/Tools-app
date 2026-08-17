package fr.huiitre.tools.modules.palworld.domain.breeding;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
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
        Set<Long> specialChildren = exceptions.stream()
                .map(BreedingException::childPalId)
                .collect(java.util.stream.Collectors.toSet());
        List<RankedCandidate> formulaCandidates = buildFormulaCandidates(allPals, specialChildren);

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

                int targetRank = Math.floorDiv(a.combiRank() + b.combiRank() + 1, 2);
                BreedingPal child = findFormulaChild(targetRank, formulaCandidates);
                results.add(new BreedingPairResult(
                        a.id(), null, b.id(), null, BreedingRule.FORMULA, child.id(),
                        new FormulaDetails(a.combiRank(), b.combiRank(), targetRank, Math.abs(child.combiRank() - targetRank))));
            }
        }

        return results;
    }

    private static List<RankedCandidate> buildFormulaCandidates(List<BreedingPal> allPals, Set<Long> specialChildren) {
        Map<Integer, RankedCandidate> bestByRank = new HashMap<>();
        for (int index = 0; index < allPals.size(); index++) {
            BreedingPal pal = allPals.get(index);
            if (pal.ignoreCombi() || pal.combiRank() == null || pal.combiDuplicatePriority() == null || specialChildren.contains(pal.id())) continue;

            RankedCandidate current = bestByRank.get(pal.combiRank());
            if (current == null || pal.combiDuplicatePriority() > current.pal().combiDuplicatePriority()) {
                bestByRank.put(pal.combiRank(), new RankedCandidate(pal, index));
            }
        }
        return bestByRank.values().stream()
                .sorted(Comparator.comparingInt(candidate -> candidate.pal().combiRank()))
                .toList();
    }

    private static BreedingPal findFormulaChild(int targetRank, List<RankedCandidate> candidates) {
        int low = 0;
        int high = candidates.size() - 1;
        while (low <= high) {
            int mid = (low + high) >>> 1;
            int rank = candidates.get(mid).pal().combiRank();
            if (rank == targetRank) return candidates.get(mid).pal();
            if (rank < targetRank) low = mid + 1;
            else high = mid - 1;
        }

        RankedCandidate lower = high >= 0 ? candidates.get(high) : null;
        RankedCandidate upper = low < candidates.size() ? candidates.get(low) : null;
        if (lower == null) return upper.pal();
        if (upper == null) return lower.pal();

        int lowerDistance = Math.abs(lower.pal().combiRank() - targetRank);
        int upperDistance = Math.abs(upper.pal().combiRank() - targetRank);
        if (upperDistance < lowerDistance) return upper.pal();
        if (lowerDistance < upperDistance) return lower.pal();
        return lower.pal().combiDuplicatePriority() > upper.pal().combiDuplicatePriority()
                ? lower.pal()
                : upper.pal();
    }

    private record SpeciesPairKey(long lo, long hi) {
        static SpeciesPairKey of(long a, long b) {
            return a <= b ? new SpeciesPairKey(a, b) : new SpeciesPairKey(b, a);
        }
    }

    private record RankedCandidate(BreedingPal pal, int index) {}
}
