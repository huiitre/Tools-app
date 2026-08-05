package fr.huiitre.tools.modules.palworld.domain.breeding;

import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

// Recherche dans un graphe ET/OU : contrairement à un graphe classique, une espèce enfant a besoin
// de SES DEUX parents disponibles simultanément, pas d'un seul chemin alternatif. Algorithme par
// point fixe : on fait grandir l'ensemble des espèces "atteignables" (au départ = les espèces
// possédées) tant qu'une nouvelle espèce devient atteignable via une de ses paires de reproduction
// dont les deux parents sont déjà atteignables. Ne cherche pas l'arbre de profondeur minimale
// (premier chemin trouvé retenu) — la profondeur n'est pas un critère ici, seule la faisabilité
// compte (cf. discussion produit : peu importe le nombre de générations nécessaires).
public final class BreedingPathBuilder {

    private BreedingPathBuilder() {}

    public static Optional<BreedingPathNode> build(Long targetSpeciesId, Set<Long> ownedSpeciesIds, List<BreedingPairResult> allPairs) {
        if (ownedSpeciesIds.contains(targetSpeciesId)) {
            return Optional.of(new BreedingPathNode(targetSpeciesId, true, null));
        }

        Map<Long, List<BreedingPairResult>> pairsByChild = allPairs.stream()
                .collect(Collectors.groupingBy(BreedingPairResult::childPalId));

        Set<Long> reachable = new HashSet<>(ownedSpeciesIds);
        Map<Long, BreedingPairResult> cameFrom = new HashMap<>();

        boolean changed = true;
        while (changed && !reachable.contains(targetSpeciesId)) {
            changed = false;
            for (Map.Entry<Long, List<BreedingPairResult>> entry : pairsByChild.entrySet()) {
                Long childId = entry.getKey();
                if (reachable.contains(childId)) continue;

                for (BreedingPairResult pair : entry.getValue()) {
                    if (reachable.contains(pair.parentAPalId()) && reachable.contains(pair.parentBPalId())) {
                        cameFrom.put(childId, pair);
                        reachable.add(childId);
                        changed = true;
                        break;
                    }
                }
            }
        }

        if (!reachable.contains(targetSpeciesId)) {
            return Optional.empty();
        }

        return Optional.of(buildNode(targetSpeciesId, ownedSpeciesIds, cameFrom));
    }

    private static BreedingPathNode buildNode(Long speciesId, Set<Long> ownedSpeciesIds, Map<Long, BreedingPairResult> cameFrom) {
        if (ownedSpeciesIds.contains(speciesId)) {
            return new BreedingPathNode(speciesId, true, null);
        }

        BreedingPairResult pair = cameFrom.get(speciesId);
        BreedingPathNode parentANode = buildNode(pair.parentAPalId(), ownedSpeciesIds, cameFrom);
        BreedingPathNode parentBNode = buildNode(pair.parentBPalId(), ownedSpeciesIds, cameFrom);
        return new BreedingPathNode(speciesId, false, new BreedingPathStep(parentANode, parentBNode, pair));
    }
}
