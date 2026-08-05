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
    @DisplayName("Le Pal cible déjà possédé est atteignable sans aucune étape")
    void should_be_trivially_reachable_when_target_already_owned() {
        long bastetIce = idOf("Bastet_Ice");

        Optional<BreedingPathNode> result = BreedingPathBuilder.build(bastetIce, Set.of(bastetIce), ALL_PAIRS);

        assertTrue(result.isPresent());
        assertTrue(result.get().owned());
        assertEquals(bastetIce, result.get().speciesId());
        assertEquals(null, result.get().step());
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

    // Chaîne vérifiée sur les vraies données : ChickenPal + DarkCrow => ColorfulBird (formule, gen 1),
    // puis ChickenPal + ColorfulBird => DreamDemon (formule, gen 2) — DreamDemon n'est PAS atteignable
    // en une seule génération depuis {ChickenPal, DarkCrow} seuls, donc ce test vérifie réellement le
    // point fixe multi-génération, pas juste le cas à une étape.
    @Test
    @DisplayName("Chemin sur deux générations : ChickenPal + DarkCrow => ColorfulBird => (+ ChickenPal) => DreamDemon")
    void should_find_multi_generation_path() {
        long chickenPal = idOf("ChickenPal");
        long darkCrow = idOf("DarkCrow");
        long colorfulBird = idOf("ColorfulBird");
        long dreamDemon = idOf("DreamDemon");

        Optional<BreedingPathNode> result = BreedingPathBuilder.build(dreamDemon, Set.of(chickenPal, darkCrow), ALL_PAIRS);

        assertTrue(result.isPresent());
        BreedingPathNode root = result.get();
        assertFalse(root.owned());
        assertEquals(dreamDemon, root.speciesId());

        BreedingPathNode parentA = root.step().parentA();
        BreedingPathNode parentB = root.step().parentB();
        Set<Long> topLevelSpecies = Set.of(parentA.speciesId(), parentB.speciesId());
        assertEquals(Set.of(chickenPal, colorfulBird), topLevelSpecies);

        // Le parent "ChickenPal" du dernier croisement est directement possédé, l'autre (ColorfulBird)
        // doit lui-même être issu d'une étape de reproduction antérieure (pas possédé directement).
        BreedingPathNode ownedParent = parentA.speciesId() == chickenPal ? parentA : parentB;
        BreedingPathNode bredParent = parentA.speciesId() == chickenPal ? parentB : parentA;
        assertTrue(ownedParent.owned());
        assertFalse(bredParent.owned());
        assertEquals(colorfulBird, bredParent.speciesId());

        Set<Long> gen1Species = Set.of(bredParent.step().parentA().speciesId(), bredParent.step().parentB().speciesId());
        assertEquals(Set.of(chickenPal, darkCrow), gen1Species);
        assertTrue(bredParent.step().parentA().owned());
        assertTrue(bredParent.step().parentB().owned());
    }
}
