package fr.huiitre.tools.modules.palworld.domain;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class WorkSuitabilityMaxLevelCalculator {

    private static final int MAX_LEVEL_CAP = 10;

    private static final List<String> CATEGORY_ORDER = List.of(
            "EmitFlame", "Watering", "Seeding", "GenerateElectricity", "Handcraft",
            "Collection", "Deforest", "Mining", "OilExtraction", "ProductMedicine",
            "Cool", "Transport", "MonsterFarm");

    public static Map<String, Integer> computeMaxLevels(String bestCategory, Map<String, Integer> levelByCategory) {
        List<String> categories = new ArrayList<>(levelByCategory.keySet());
        Map<String, Integer> individualBonus = new LinkedHashMap<>();
        categories.forEach(category -> individualBonus.put(category, 0));

        int categoryCount = categories.size();
        if (categoryCount <= 3) {
            categories.forEach(category -> individualBonus.put(category, 1));
            if (categoryCount == 1) individualBonus.put(bestCategory, 3);
            else if (categoryCount == 2) individualBonus.put(bestCategory, 2);
            // categoryCount == 3 : chaque aptitude reste à 1
        } else {
            List<String> others = categories.stream()
                    .filter(category -> !category.equals(bestCategory))
                    .sorted(Comparator.<String>comparingInt(category -> -levelByCategory.get(category))
                            .thenComparingInt(CATEGORY_ORDER::indexOf))
                    .toList();
            individualBonus.put(bestCategory, 1);
            others.stream().limit(2).forEach(category -> individualBonus.put(category, 1));
        }

        Map<String, Integer> result = new LinkedHashMap<>();
        categories.forEach(category -> result.put(category,
                Math.min(MAX_LEVEL_CAP, levelByCategory.get(category) + individualBonus.get(category) + 1)));
        return result;
    }
}
