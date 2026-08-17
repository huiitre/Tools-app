package fr.huiitre.tools.modules.palworld.domain;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.LinkedHashMap;
import java.util.Map;
import java.util.stream.Stream;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

@Tag("PALWORLD")
@DisplayName("Palworld - WorkSuitabilityMaxLevelCalculator")
class WorkSuitabilityMaxLevelCalculatorTest {

    @ParameterizedTest(name = "{0}")
    @MethodSource("validationCases")
    @DisplayName("Doit calculer le niveau max à 4 étoiles conforme à paldb.cc (19 Pals vérifiés manuellement)")
    void should_compute_max_level_matching_paldb_cc(String name, String best, Map<String, Integer> base,
            Map<String, Integer> expected) {

        // * when */
        Map<String, Integer> result = WorkSuitabilityMaxLevelCalculator.computeMaxLevels(best, base);

        // * then */
        assertEquals(expected, result);
    }

    private static Stream<Arguments> validationCases() {
        return Stream.of(
                testCase("Chikipi", "MonsterFarm", map("Collection", 1, "MonsterFarm", 1),
                        map("Collection", 3, "MonsterFarm", 4)),
                testCase("Vixy", "MonsterFarm", map("Collection", 1, "MonsterFarm", 1),
                        map("Collection", 3, "MonsterFarm", 4)),
                testCase("Celaray", "Watering", map("Watering", 1, "Transport", 1),
                        map("Watering", 4, "Transport", 3)),
                testCase("Tropicaw", "Seeding", map("Seeding", 5, "Collection", 5),
                        map("Seeding", 8, "Collection", 7)),
                testCase("Clovee", "Seeding", map("Seeding", 1, "Collection", 1),
                        map("Seeding", 4, "Collection", 3)),
                testCase("Robinquill", "Handcraft",
                        map("Seeding", 2, "Handcraft", 3, "Collection", 3, "Deforest", 2, "ProductMedicine", 1, "Transport", 2),
                        map("Seeding", 4, "Handcraft", 5, "Collection", 5, "Deforest", 3, "ProductMedicine", 2, "Transport", 3)),
                testCase("Leafan", "Seeding", map("Seeding", 3, "Handcraft", 3, "Collection", 3, "Transport", 2),
                        map("Seeding", 5, "Handcraft", 5, "Collection", 5, "Transport", 3)),
                testCase("Fuack Ignis", "EmitFlame", map("EmitFlame", 2, "Watering", 2, "Handcraft", 1, "Transport", 1),
                        map("EmitFlame", 4, "Watering", 4, "Handcraft", 3, "Transport", 2)),
                testCase("Verdash", "Handcraft", map("Seeding", 4, "Handcraft", 5, "Collection", 5, "Deforest", 3, "Transport", 3),
                        map("Seeding", 6, "Handcraft", 7, "Collection", 7, "Deforest", 4, "Transport", 4)),
                testCase("Vaelet", "MonsterFarm",
                        map("Seeding", 3, "Handcraft", 3, "Collection", 3, "ProductMedicine", 3, "Transport", 2, "MonsterFarm", 2),
                        map("Seeding", 5, "Handcraft", 5, "Collection", 4, "ProductMedicine", 4, "Transport", 3, "MonsterFarm", 4)),
                testCase("Icelyn", "Handcraft", map("Handcraft", 5, "ProductMedicine", 4, "Cool", 5, "Transport", 2),
                        map("Handcraft", 7, "ProductMedicine", 6, "Cool", 7, "Transport", 3)),
                testCase("Beegarde", "MonsterFarm",
                        map("Seeding", 2, "Handcraft", 2, "Collection", 3, "Deforest", 2, "ProductMedicine", 2, "Transport", 2, "MonsterFarm", 3),
                        map("Seeding", 4, "Handcraft", 3, "Collection", 5, "Deforest", 3, "ProductMedicine", 3, "Transport", 3, "MonsterFarm", 5)),
                testCase("Flambelle", "MonsterFarm", map("EmitFlame", 1, "Handcraft", 1, "Transport", 1, "MonsterFarm", 1),
                        map("EmitFlame", 3, "Handcraft", 3, "Transport", 2, "MonsterFarm", 3)),
                testCase("Pengullet", "Watering", map("Watering", 1, "Handcraft", 1, "Cool", 1, "Transport", 1),
                        map("Watering", 3, "Handcraft", 3, "Cool", 3, "Transport", 2)),
                testCase("Quivern Botan", "Seeding", map("Seeding", 5, "Handcraft", 2, "Collection", 4, "Mining", 3, "Transport", 4),
                        map("Seeding", 7, "Handcraft", 3, "Collection", 6, "Mining", 4, "Transport", 6)),
                testCase("Sootseer", "MonsterFarm", map("EmitFlame", 3, "Handcraft", 3, "Collection", 2, "Mining", 3, "MonsterFarm", 2),
                        map("EmitFlame", 5, "Handcraft", 5, "Collection", 3, "Mining", 4, "MonsterFarm", 4)),
                testCase("Lifmunk", "Seeding", map("Seeding", 1, "Handcraft", 1, "Collection", 1, "Deforest", 1, "ProductMedicine", 1),
                        map("Seeding", 3, "Handcraft", 3, "Collection", 3, "Deforest", 2, "ProductMedicine", 2)),
                testCase("Dazzi Noct", "Transport", map("GenerateElectricity", 1, "Handcraft", 1, "ProductMedicine", 1, "Transport", 2),
                        map("GenerateElectricity", 3, "Handcraft", 3, "ProductMedicine", 2, "Transport", 4)),
                testCase("Jetragon (plafond à 10, pas 12)", "Collection", map("Collection", 8),
                        map("Collection", 10)));
    }

    private static Arguments testCase(String name, String best, Map<String, Integer> base, Map<String, Integer> expected) {
        return Arguments.of(name, best, base, expected);
    }

    private static Map<String, Integer> map(Object... entries) {
        Map<String, Integer> result = new LinkedHashMap<>();
        for (int i = 0; i < entries.length; i += 2) {
            result.put((String) entries[i], (Integer) entries[i + 1]);
        }
        return result;
    }
}
