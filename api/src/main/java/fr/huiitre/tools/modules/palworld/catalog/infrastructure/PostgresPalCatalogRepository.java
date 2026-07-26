package fr.huiitre.tools.modules.palworld.catalog.infrastructure;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.catalog.application.ports.PalCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.view.ElementSummaryView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.PalListItemView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.WorkSuitabilitySummaryView;

public class PostgresPalCatalogRepository implements PalCatalogRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresPalCatalogRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public List<PalListItemView> findAll() {
        Map<Long, List<ElementSummaryView>> elementsByPalId = new LinkedHashMap<>();
        final String elementsSql = """
                SELECT pe.pal_id, e.id, e.name, e.icon_url
                FROM tools_palworld.pal_element pe
                JOIN tools_palworld.element e ON e.id = pe.element_id
                ORDER BY pe.pal_id, pe.sort_order
                """;
        jdbcTemplate.query(elementsSql, rs -> {
            elementsByPalId.computeIfAbsent(rs.getLong("pal_id"), id -> new ArrayList<>())
                    .add(new ElementSummaryView(rs.getLong("id"), rs.getString("name"), rs.getString("icon_url")));
        });

        Map<Long, List<WorkSuitabilitySummaryView>> workSuitabilitiesByPalId = new LinkedHashMap<>();
        final String workSuitabilitiesSql = """
                SELECT pws.pal_id, ws.id, ws.slug, ws.name, ws.icon_url, pws.level
                FROM tools_palworld.pal_work_suitability pws
                JOIN tools_palworld.work_suitability ws ON ws.id = pws.work_suitability_id
                ORDER BY pws.pal_id, pws.level DESC
                """;
        jdbcTemplate.query(workSuitabilitiesSql, rs -> {
            workSuitabilitiesByPalId.computeIfAbsent(rs.getLong("pal_id"), id -> new ArrayList<>())
                    .add(new WorkSuitabilitySummaryView(rs.getLong("id"), rs.getString("slug"), rs.getString("name"),
                            rs.getString("icon_url"), rs.getInt("level")));
        });

        final String palsSql = """
                SELECT id, tribe, paldex_index, paldex_suffix, name, image_url, rarity, size, base_hp, base_attack,
                       base_defense, base_work_speed, base_support, food_amount, best_work_suitability_label
                FROM tools_palworld.pal
                ORDER BY paldex_index, paldex_suffix
                """;
        return jdbcTemplate.query(palsSql, (rs, rowNum) -> {
            Long id = rs.getLong("id");
            return new PalListItemView(
                    id,
                    rs.getString("tribe"),
                    (Integer) rs.getObject("paldex_index"),
                    rs.getString("paldex_suffix"),
                    rs.getString("name"),
                    rs.getString("image_url"),
                    (Integer) rs.getObject("rarity"),
                    rs.getString("size"),
                    (Integer) rs.getObject("base_hp"),
                    (Integer) rs.getObject("base_attack"),
                    (Integer) rs.getObject("base_defense"),
                    (Integer) rs.getObject("base_work_speed"),
                    (Integer) rs.getObject("base_support"),
                    (Integer) rs.getObject("food_amount"),
                    rs.getString("best_work_suitability_label"),
                    elementsByPalId.getOrDefault(id, List.of()),
                    workSuitabilitiesByPalId.getOrDefault(id, List.of()));
        });
    }
}
