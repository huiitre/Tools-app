package fr.huiitre.tools.modules.palworld.domain.breeding;

import java.util.List;

public final class BreedingEngine {

    private BreedingEngine() {}

    public static BreedingComputation compute(
            BreedingPal parentA,
            Gender genderA,
            BreedingPal parentB,
            Gender genderB,
            List<BreedingException> exceptions,
            List<BreedingPal> allPals) {

        BreedingComputation exceptionMatch = matchException(parentA.id(), genderA, parentB.id(), genderB, exceptions);
        if (exceptionMatch != null) {
            return exceptionMatch;
        }

        int targetRank = Math.floorDiv(parentA.combiRank() + parentB.combiRank() + 1, 2);
        BreedingPal best = null;
        int bestDistance = Integer.MAX_VALUE;

        for (BreedingPal candidate : allPals) {
            if (candidate.ignoreCombi() || candidate.combiRank() == null) continue;

            int distance = Math.abs(candidate.combiRank() - targetRank);
            if (best == null) {
                best = candidate;
                bestDistance = distance;
            } else if (distance < bestDistance
                    || (distance == bestDistance && candidate.combiDuplicatePriority() > best.combiDuplicatePriority())) {
                best = candidate;
                bestDistance = distance;
            }
        }

        if (best == null) {
            throw new IllegalStateException("No eligible Palworld species found for breeding formula (target rank " + targetRank + ")");
        }

        return BreedingComputation.formula(best.id(),
                new FormulaDetails(parentA.combiRank(), parentB.combiRank(), targetRank, bestDistance));
    }

    // Une exception ne "connait" pas parentA/parentB de l'appelant : elle stocke sa propre paire ordonnée.
    // On teste les deux permutations (A,B) et (B,A) car parentA/parentB ne sont que des étiquettes d'entrée
    // pour une paire non-ordonnée de deux individus. Le sexe attribué à chaque étiquette d'appel est
    // recalculé selon la permutation qui a matché, pas simplement recopié depuis l'exception.
    private static BreedingComputation matchException(
            Long aId, Gender genderA, Long bId, Gender genderB, List<BreedingException> exceptions) {
        for (BreedingException e : exceptions) {
            if (matchesSlot(e.parentAPalId(), e.parentAGender(), aId, genderA)
                    && matchesSlot(e.parentBPalId(), e.parentBGender(), bId, genderB)) {
                return BreedingComputation.exception(e.childPalId(), e.parentAGender(), e.parentBGender());
            }
            if (matchesSlot(e.parentAPalId(), e.parentAGender(), bId, genderB)
                    && matchesSlot(e.parentBPalId(), e.parentBGender(), aId, genderA)) {
                return BreedingComputation.exception(e.childPalId(), e.parentBGender(), e.parentAGender());
            }
        }
        return null;
    }

    private static boolean matchesSlot(Long exceptionPalId, Gender exceptionGender, Long inputPalId, Gender inputGender) {
        if (!exceptionPalId.equals(inputPalId)) return false;
        return exceptionGender == null || exceptionGender == inputGender;
    }
}
