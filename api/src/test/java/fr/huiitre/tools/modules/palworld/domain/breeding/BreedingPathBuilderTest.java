package fr.huiitre.tools.modules.palworld.domain.breeding;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

@Tag("PALWORLD")
@DisplayName("Palworld - BreedingPathBuilder")
class BreedingPathBuilderTest {

    private static final List<BreedingPal> ALL_PALS = BreedingFixtures.loadAllPals();
    private static final Map<String, Long> TRIBE_TO_ID = BreedingFixtures.tribeToId(ALL_PALS);
    private static final List<BreedingException> VALID_EXCEPTIONS = BreedingFixtures.loadValidExceptions(TRIBE_TO_ID);
    private static final List<BreedingPairResult> ALL_PAIRS = BreedingIndexBuilder.buildAll(ALL_PALS, VALID_EXCEPTIONS);

    private long idOf(String tribe) {
        return BreedingFixtures.byTribe(ALL_PALS, tribe).id();
    }

    @Test
    @DisplayName("Le Pal cible déjà possédé propose son auto-reproduction")
    void should_propose_self_breeding_when_target_already_owned() {
        long bastetIce = idOf("Bastet_Ice");

        Optional<BreedingPathNode> result = BreedingPathBuilder.build(bastetIce, Set.of(bastetIce), ALL_PAIRS);

        assertTrue(result.isPresent());
        assertFalse(result.get().owned());
        assertEquals(bastetIce, result.get().speciesId());
        assertEquals(bastetIce, result.get().step().parentA().speciesId());
        assertEquals(bastetIce, result.get().step().parentB().speciesId());
        assertTrue(result.get().step().parentA().owned());
        assertTrue(result.get().step().parentB().owned());
    }

    @Test
    @DisplayName("Exception directe : posséder Mau(Bastet) + Pengullet(Penguin) permet d'obtenir Mau Cryst(Bastet_Ice) en une étape")
    void should_find_one_step_path_via_exception() {
        long bastet = idOf("Bastet");
        long penguin = idOf("Penguin");
        long bastetIce = idOf("Bastet_Ice");

        Optional<BreedingPathNode> result = BreedingPathBuilder.build(bastetIce, Set.of(bastet, penguin), ALL_PAIRS);

        assertTrue(result.isPresent());
        BreedingPathNode node = result.get();
        assertFalse(node.owned());
        assertEquals(bastetIce, node.speciesId());
        assertEquals(BreedingRule.EXCEPTION, node.step().pair().rule());

        Set<Long> usedParents = Set.of(node.step().parentA().speciesId(), node.step().parentB().speciesId());
        assertEquals(Set.of(bastet, penguin), usedParents);
        assertTrue(node.step().parentA().owned());
        assertTrue(node.step().parentB().owned());
    }

    @Test
    @DisplayName("Sans aucun Pal possédé, rien n'est jamais atteignable")
    void should_be_unreachable_when_nothing_is_owned() {
        long bastetIce = idOf("Bastet_Ice");

        Optional<BreedingPathNode> result = BreedingPathBuilder.build(bastetIce, Set.of(), ALL_PAIRS);

        assertTrue(result.isEmpty());
    }

    @Test
    @DisplayName("Un chemin multi-génération est construit depuis les Pals possédés")
    void should_find_multi_generation_path() {
        long chickenPal = idOf("ChickenPal");
        long darkCrow = idOf("DarkCrow");
        long dreamDemon = idOf("DreamDemon");

        Optional<BreedingPathNode> result = BreedingPathBuilder.build(dreamDemon, Set.of(chickenPal, darkCrow), ALL_PAIRS);

        assertTrue(result.isPresent());
        BreedingPathNode root = result.get();
        assertFalse(root.owned());
        assertEquals(dreamDemon, root.speciesId());

        assertTrue(depth(root) >= 2);
        assertLeavesAreOwned(root, Set.of(chickenPal, darkCrow));
    }

    private int depth(BreedingPathNode node) {
        if (node.step() == null) return 0;
        return 1 + Math.max(depth(node.step().parentA()), depth(node.step().parentB()));
    }

    private void assertLeavesAreOwned(BreedingPathNode node, Set<Long> ownedSpeciesIds) {
        if (node.step() == null) {
            assertTrue(node.owned());
            assertTrue(ownedSpeciesIds.contains(node.speciesId()));
            return;
        }
        assertLeavesAreOwned(node.step().parentA(), ownedSpeciesIds);
        assertLeavesAreOwned(node.step().parentB(), ownedSpeciesIds);
    }
}
