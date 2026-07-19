package fr.huiitre.tools.modules.palworld.tierlist.infrastructure;

import fr.huiitre.tools.modules.palworld.tierlist.application.ports.PalworldTierListProvider;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.PalworldPalView;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.PalworldSpeedView;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.PalworldTierGroupView;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.PalworldWorkSkillView;
import org.jsoup.Jsoup;
import org.jsoup.nodes.Document;
import org.jsoup.nodes.Element;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class PalworldGgTierListScraper implements PalworldTierListProvider {

    private static final Logger log = LoggerFactory.getLogger(PalworldGgTierListScraper.class);

    private static final String BASE_URL = "https://palworld.gg";
    private static final List<String> TIER_ORDER = List.of("S", "A", "B", "C", "D");
    private static final String USER_AGENT =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private static final String CATEGORY_BASE_WORK = "base-work";
    private static final List<String> SPEED_CATEGORIES = List.of("flying-mounts", "ground-mounts");

    private static final Map<String, String> CATEGORY_PATHS = new LinkedHashMap<>();
    static {
        CATEGORY_PATHS.put("best", "/fr/tier-list");
        CATEGORY_PATHS.put(CATEGORY_BASE_WORK, "/fr/tier-list/base-work");
        CATEGORY_PATHS.put("flying-mounts", "/fr/tier-list/flying-mounts");
        CATEGORY_PATHS.put("ground-mounts", "/fr/tier-list/ground-mounts");
        CATEGORY_PATHS.put("combat", "/fr/tier-list/combat");
    }

    @Override
    public Map<String, List<PalworldTierGroupView>> getTierLists() {
        Map<String, List<PalworldTierGroupView>> result = new LinkedHashMap<>();
        for (Map.Entry<String, String> entry : CATEGORY_PATHS.entrySet()) {
            try {
                result.put(entry.getKey(), scrapeCategory(entry.getKey(), entry.getValue()));
            } catch (IOException e) {
                log.warn("Échec du scraping de la tier list Palworld ({}) : {}", entry.getKey(), e.getMessage());
                result.put(entry.getKey(), List.of());
            }
        }
        return result;
    }

    private List<PalworldTierGroupView> scrapeCategory(String category, String path) throws IOException {
        Document doc = Jsoup.connect(BASE_URL + path)
                .userAgent(USER_AGENT)
                .header("Accept-Language", "fr-FR,fr;q=0.9,en-US;q=0.8")
                .timeout(10_000)
                .get();

        List<PalworldTierGroupView> tiers = new ArrayList<>();

        for (Element tierEl : doc.select(".tier-list .tier")) {
            Element nameEl = tierEl.selectFirst(".t-name");
            String tier = nameEl != null ? nameEl.text().trim() : "";
            if (!TIER_ORDER.contains(tier)) continue;

            List<PalworldPalView> pals = new ArrayList<>();
            for (Element palEl : tierEl.select(".pal")) {
                Element img = palEl.selectFirst("img");
                if (img == null) continue;

                Element link = palEl.selectFirst("a.link");

                String name = img.attr("alt");
                String src = !img.attr("src").isBlank() ? img.attr("src") : img.attr("srcset").split(" ")[0];
                String href = link != null ? link.attr("href") : "";

                if (name.isBlank() || src.isBlank()) continue;

                String originalPath = src.replaceFirst("^/_ipx/[^/]+", "");
                String image = BASE_URL + originalPath;
                String palUrl = !href.isBlank() ? BASE_URL + href : "#";

                List<PalworldWorkSkillView> workSkills = CATEGORY_BASE_WORK.equals(category)
                        ? parseWorkSkills(palEl) : null;
                PalworldSpeedView speed = SPEED_CATEGORIES.contains(category)
                        ? parseSpeed(palEl) : null;

                pals.add(new PalworldPalView(name, image, palUrl, workSkills, speed));
            }

            if (!pals.isEmpty()) tiers.add(new PalworldTierGroupView(tier, pals));
        }

        tiers.sort(Comparator.comparingInt(t -> TIER_ORDER.indexOf(t.tier())));
        return tiers;
    }

    private List<PalworldWorkSkillView> parseWorkSkills(Element palEl) {
        List<PalworldWorkSkillView> workSkills = new ArrayList<>();
        for (Element itemEl : palEl.select(".works .items .item")) {
            Element iconImg = itemEl.selectFirst(".w .image img");
            Element levelValue = itemEl.selectFirst(".level .value");
            if (iconImg == null || levelValue == null) continue;

            String skillName = iconImg.attr("alt");
            String icon = BASE_URL + iconImg.attr("src");
            try {
                int level = Integer.parseInt(levelValue.text().trim());
                workSkills.add(new PalworldWorkSkillView(skillName, icon, level));
            } catch (NumberFormatException e) {
                log.warn("Niveau de compétence métier illisible pour {} : {}", skillName, levelValue.text());
            }
        }
        return workSkills;
    }

    private PalworldSpeedView parseSpeed(Element palEl) {
        Element speedEl = palEl.selectFirst(".speed");
        if (speedEl == null) return null;

        String[] parts = speedEl.text().trim().split("-");
        if (parts.length != 2) return null;

        try {
            int min = Integer.parseInt(parts[0].trim());
            int max = Integer.parseInt(parts[1].trim());
            return new PalworldSpeedView(min, max);
        } catch (NumberFormatException e) {
            log.warn("Vitesse illisible : {}", speedEl.text());
            return null;
        }
    }
}
