package fr.huiitre.tools.modules.palworld.domain.breeding;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.stream.Stream;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingFixtures.RawExceptionEntry;

@Tag("PALWORLD")
@DisplayName("Palworld - BreedingEngine")
class BreedingEngineTest {

    private static final List<BreedingPal> ALL_PALS = BreedingFixtures.loadAllPals();
    private static final Map<String, Long> TRIBE_TO_ID = BreedingFixtures.tribeToId(ALL_PALS);
    private static final List<BreedingException> VALID_EXCEPTIONS = BreedingFixtures.loadValidExceptions(TRIBE_TO_ID);

    // Les 3 seules références de breeding.json sans équivalent dans pals.json (confirmé sur les vrais
    // assets 2026-08-05) : WindChimes/WindChimes_Ice (aucun équivalent pak connu) et Blueplatypus (variante
    // de casse de "BluePlatypus" côté pak, cf. mémoire projet palworld-extractor-pak-migration).
    private static final Set<String> KNOWN_UNRESOLVABLE_TRIBES = Set.of("WindChimes", "WindChimes_Ice", "Blueplatypus");

    @Test
    @DisplayName("Formule + départage : Chikipi(ChickenPal) + Cawgnito(DarkCrow) => Tocotoco(ColorfulBird)")
    void should_apply_formula_and_tiebreak_for_chikipi_and_cawgnito() {
        // NB: l'exemple donné dans la spec métier ("Chikipi + Nox") utilise un combiRank (2370) qui, sur les
        // vrais assets, appartient à Cawgnito (DarkCrow) et non Nox (NightFox, combiRank réel 2920) — le calcul
        // (target 2725, Tocotoco 2730 vs Melpaca 2720, distance 5 de part et d'autre, Tocotoco gagne au
        // departage 273000 > 272000) est vérifié ici avec les vraies données, donc avec la bonne espèce.
        BreedingPal chikipi = BreedingFixtures.byTribe(ALL_PALS, "ChickenPal");
        BreedingPal cawgnito = BreedingFixtures.byTribe(ALL_PALS, "DarkCrow");
        BreedingPal tocotoco = BreedingFixtures.byTribe(ALL_PALS, "ColorfulBird");

        assertEquals(3080, chikipi.combiRank());
        assertEquals(2370, cawgnito.combiRank());

        BreedingComputation result = BreedingEngine.compute(chikipi, null, cawgnito, null, VALID_EXCEPTIONS, ALL_PALS);

        assertEquals(BreedingRule.FORMULA, result.rule());
        assertEquals(tocotoco.id(), result.childPalId());
        assertEquals(2725, result.formulaDetails().targetRank());
        assertEquals(5, result.formulaDetails().distance());
    }

    @Test
    @DisplayName("Exception fixe : Mau(Bastet) + Pengullet(Penguin) => Mau Cryst(Bastet_Ice), sans passer par la formule")
    void should_apply_fixed_exception_for_mau_and_pengullet() {
        BreedingPal mau = BreedingFixtures.byTribe(ALL_PALS, "Bastet");
        BreedingPal pengullet = BreedingFixtures.byTribe(ALL_PALS, "Penguin");
        BreedingPal mauCryst = BreedingFixtures.byTribe(ALL_PALS, "Bastet_Ice");

        BreedingComputation result = BreedingEngine.compute(mau, null, pengullet, null, VALID_EXCEPTIONS, ALL_PALS);

        assertEquals(BreedingRule.EXCEPTION, result.rule());
        assertEquals(mauCryst.id(), result.childPalId());
    }

    @Test
    @DisplayName("Exception sexuée : Katress(CatMage) mâle + Wixen(FoxMage) femelle => Wixen Noct(FoxMage_Dark)")
    void should_apply_male_catmage_female_foxmage_exception() {
        BreedingPal catMage = BreedingFixtures.byTribe(ALL_PALS, "CatMage");
        BreedingPal foxMage = BreedingFixtures.byTribe(ALL_PALS, "FoxMage");
        BreedingPal foxMageDark = BreedingFixtures.byTribe(ALL_PALS, "FoxMage_Dark");

        BreedingComputation result = BreedingEngine.compute(catMage, Gender.MALE, foxMage, Gender.FEMALE, VALID_EXCEPTIONS, ALL_PALS);

        assertEquals(BreedingRule.EXCEPTION, result.rule());
        assertEquals(foxMageDark.id(), result.childPalId());
        assertEquals(Gender.MALE, result.exceptionParentAGender());
        assertEquals(Gender.FEMALE, result.exceptionParentBGender());
    }

    @Test
    @DisplayName("Exception sexuée : Katress(CatMage) femelle + Wixen(FoxMage) mâle => Katress Ignis(CatMage_Fire)")
    void should_apply_female_catmage_male_foxmage_exception() {
        BreedingPal catMage = BreedingFixtures.byTribe(ALL_PALS, "CatMage");
        BreedingPal foxMage = BreedingFixtures.byTribe(ALL_PALS, "FoxMage");
        BreedingPal catMageFire = BreedingFixtures.byTribe(ALL_PALS, "CatMage_Fire");

        BreedingComputation result = BreedingEngine.compute(catMage, Gender.FEMALE, foxMage, Gender.MALE, VALID_EXCEPTIONS, ALL_PALS);

        assertEquals(BreedingRule.EXCEPTION, result.rule());
        assertEquals(catMageFire.id(), result.childPalId());
    }

    // Vérifie aussi l'ordre inversé (parentA/parentB ne sont que des étiquettes d'entrée) : appeler avec
    // Wixen en A et Katress en B doit donner exactement le même résultat.
    @Test
    @DisplayName("L'exception sexuée fonctionne aussi avec les parents inversés (A/B ne sont que des étiquettes)")
    void should_apply_exception_regardless_of_caller_argument_order() {
        BreedingPal catMage = BreedingFixtures.byTribe(ALL_PALS, "CatMage");
        BreedingPal foxMage = BreedingFixtures.byTribe(ALL_PALS, "FoxMage");
        BreedingPal foxMageDark = BreedingFixtures.byTribe(ALL_PALS, "FoxMage_Dark");

        BreedingComputation result = BreedingEngine.compute(foxMage, Gender.FEMALE, catMage, Gender.MALE, VALID_EXCEPTIONS, ALL_PALS);

        assertEquals(BreedingRule.EXCEPTION, result.rule());
        assertEquals(foxMageDark.id(), result.childPalId());
    }

    @ParameterizedTest(name = "{0} + {1} => {2}")
    @MethodSource("everyValidBreedingJsonLine")
    @DisplayName("Chaque ligne résolvable de breeding.json produit exactement l'enfant attendu, via une exception")
    void should_match_every_valid_breeding_json_line(String parentATribe, String parentBTribe, String childTribe,
            String genderACode, String genderBCode) {
        BreedingPal parentA = BreedingFixtures.byTribe(ALL_PALS, parentATribe);
        BreedingPal parentB = BreedingFixtures.byTribe(ALL_PALS, parentBTribe);
        BreedingPal child = BreedingFixtures.byTribe(ALL_PALS, childTribe);

        BreedingComputation result = BreedingEngine.compute(
                parentA, Gender.fromCode(genderACode), parentB, Gender.fromCode(genderBCode), VALID_EXCEPTIONS, ALL_PALS);

        assertEquals(BreedingRule.EXCEPTION, result.rule());
        assertEquals(child.id(), result.childPalId());
    }

    private static Stream<Arguments> everyValidBreedingJsonLine() {
        List<Arguments> args = new ArrayList<>();
        for (RawExceptionEntry e : BreedingFixtures.loadRawExceptionEntries()) {
            if (!TRIBE_TO_ID.containsKey(e.parentATribe()) || !TRIBE_TO_ID.containsKey(e.parentBTribe())
                    || !TRIBE_TO_ID.containsKey(e.childTribe())) {
                continue;
            }
            args.add(Arguments.of(e.parentATribe(), e.parentBTribe(), e.childTribe(), e.parentAGenderCode(), e.parentBGenderCode()));
        }
        return args.stream();
    }

    @Test
    @DisplayName("Intégrité : les seules références invalides de breeding.json sont les 3 connues (WindChimes x2, Blueplatypus)")
    void should_only_have_known_unresolvable_references() {
        Set<String> unresolvable = new HashSet<>();
        for (RawExceptionEntry e : BreedingFixtures.loadRawExceptionEntries()) {
            for (String tribe : List.of(e.parentATribe(), e.parentBTribe(), e.childTribe())) {
                if (!TRIBE_TO_ID.containsKey(tribe)) {
                    unresolvable.add(tribe);
                }
            }
        }
        assertEquals(KNOWN_UNRESOLVABLE_TRIBES, unresolvable,
                "De nouvelles références invalides sont apparues dans breeding.json (ou une des 3 connues a été corrigée) : "
                        + "vérifier PalworldLocalBreedingExceptionDataProvider / SyncBreedingExceptionsUseCase avant de mettre à jour cette liste.");
    }

    @Test
    @DisplayName("Cohérence : le calcul direct et l'index en mémoire donnent toujours le même résultat, pour toute paire du catalogue")
    void should_keep_direct_computation_and_reverse_index_consistent() {
        List<BreedingPairResult> index = BreedingIndexBuilder.buildAll(ALL_PALS, VALID_EXCEPTIONS);
        Map<Long, BreedingPal> byId = ALL_PALS.stream()
                .collect(java.util.stream.Collectors.toMap(BreedingPal::id, p -> p));

        assertTrue(index.size() > 47000, "L'index devrait couvrir toutes les paires non-ordonnées de 309 espèces");

        for (BreedingPairResult pair : index) {
            BreedingPal parentA = byId.get(pair.parentAPalId());
            BreedingPal parentB = byId.get(pair.parentBPalId());

            BreedingComputation direct = BreedingEngine.compute(
                    parentA, pair.parentAGender(), parentB, pair.parentBGender(), VALID_EXCEPTIONS, ALL_PALS);

            assertEquals(pair.rule(), direct.rule(),
                    () -> "Incohérence de règle pour " + parentA.tribe() + " + " + parentB.tribe());
            assertEquals(pair.childPalId(), direct.childPalId(),
                    () -> "Incohérence d'enfant pour " + parentA.tribe() + " + " + parentB.tribe());
        }
    }
}
