package fr.huiitre.tools.modules.palworld.tierlist.infrastructure;

import fr.huiitre.tools.modules.palworld.tierlist.application.ports.PalworldTierListProvider;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.PalworldPalView;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.PalworldTierGroupView;
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

    private static final Map<String, String> CATEGORY_PATHS = new LinkedHashMap<>();
    static {
        CATEGORY_PATHS.put("best", "/fr/tier-list");
        CATEGORY_PATHS.put("base-work", "/fr/tier-list/base-work");
        CATEGORY_PATHS.put("flying-mounts", "/fr/tier-list/flying-mounts");
        CATEGORY_PATHS.put("ground-mounts", "/fr/tier-list/ground-mounts");
        CATEGORY_PATHS.put("combat", "/fr/tier-list/combat");
    }

    @Override
    public Map<String, List<PalworldTierGroupView>> getTierLists() {
        Map<String, List<PalworldTierGroupView>> result = new LinkedHashMap<>();
        for (Map.Entry<String, String> entry : CATEGORY_PATHS.entrySet()) {
            try {
                result.put(entry.getKey(), scrapeCategory(entry.getValue()));
            } catch (IOException e) {
                log.warn("Échec du scraping de la tier list Palworld ({}) : {}", entry.getKey(), e.getMessage());
                result.put(entry.getKey(), List.of());
            }
        }
        return result;
    }

    private List<PalworldTierGroupView> scrapeCategory(String path) throws IOException {
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

                pals.add(new PalworldPalView(name, image, palUrl));
            }

            if (!pals.isEmpty()) tiers.add(new PalworldTierGroupView(tier, pals));
        }

        tiers.sort(Comparator.comparingInt(t -> TIER_ORDER.indexOf(t.tier())));
        return tiers;
    }
}
