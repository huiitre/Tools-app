package fr.huiitre.tools.modules.palworld.catalog.infrastructure;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.catalog.application.ports.PalCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.view.ActiveSkillSummaryView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.DropSummaryView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.ElementSummaryView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.PalListItemView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.PartnerSkillRankSummaryView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.PartnerSkillSummaryView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.PassiveSkillSummaryView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.SpawnZoneSummaryView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.VariantSummaryView;
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

        Map<Long, List<PassiveSkillSummaryView>> passiveSkillsByPalId = new LinkedHashMap<>();
        final String passiveSkillsSql = """
                SELECT pal_id, id, name, tooltip, rank_icon_url
                FROM tools_palworld.pal_passive_skill
                ORDER BY pal_id, id
                """;
        jdbcTemplate.query(passiveSkillsSql, rs -> {
            passiveSkillsByPalId.computeIfAbsent(rs.getLong("pal_id"), id -> new ArrayList<>())
                    .add(new PassiveSkillSummaryView(rs.getLong("id"), rs.getString("name"), rs.getString("tooltip"),
                            rs.getString("rank_icon_url")));
        });

        Map<Long, List<ActiveSkillSummaryView>> activeSkillsByPalId = new LinkedHashMap<>();
        final String activeSkillsSql = """
                SELECT pas.pal_id, pas.unlock_level,
                       s.id AS skill_id, s.slug AS skill_slug, s.category AS skill_category, s.name AS skill_name,
                       s.icon_url AS skill_icon_url, s.cooldown AS skill_cooldown, s.power AS skill_power,
                       s.status_effect AS skill_status_effect, s.description AS skill_description,
                       e.id AS element_id, e.name AS element_name, e.icon_url AS element_icon_url
                FROM tools_palworld.pal_active_skill pas
                JOIN tools_palworld.skill s ON s.id = pas.skill_id
                LEFT JOIN tools_palworld.element e ON e.id = s.element_id
                ORDER BY pas.pal_id, pas.sort_order
                """;
        jdbcTemplate.query(activeSkillsSql, rs -> {
            Long elementId = (Long) rs.getObject("element_id");
            ElementSummaryView element = elementId == null
                    ? null
                    : new ElementSummaryView(elementId, rs.getString("element_name"), rs.getString("element_icon_url"));
            activeSkillsByPalId.computeIfAbsent(rs.getLong("pal_id"), id -> new ArrayList<>())
                    .add(new ActiveSkillSummaryView(
                            rs.getLong("skill_id"),
                            rs.getString("skill_slug"),
                            rs.getString("skill_category"),
                            rs.getString("skill_name"),
                            rs.getString("skill_icon_url"),
                            element,
                            rs.getBigDecimal("skill_cooldown"),
                            (Integer) rs.getObject("skill_power"),
                            rs.getString("skill_status_effect"),
                            rs.getString("skill_description"),
                            rs.getInt("unlock_level")));
        });

        Map<Long, List<PartnerSkillRankSummaryView>> partnerSkillRanksByPalId = new LinkedHashMap<>();
        final String partnerSkillRanksSql = """
                SELECT pal_id, sort_order, level_label, detail
                FROM tools_palworld.pal_partner_skill_rank
                ORDER BY pal_id, sort_order
                """;
        jdbcTemplate.query(partnerSkillRanksSql, rs -> {
            partnerSkillRanksByPalId.computeIfAbsent(rs.getLong("pal_id"), id -> new ArrayList<>())
                    .add(new PartnerSkillRankSummaryView(rs.getInt("sort_order"), rs.getString("level_label"),
                            rs.getString("detail")));
        });

        Map<Long, PartnerSkillSummaryView> partnerSkillByPalId = new LinkedHashMap<>();
        final String partnerSkillSql = """
                SELECT pal_id, title, description, icon_url
                FROM tools_palworld.pal_partner_skill
                """;
        jdbcTemplate.query(partnerSkillSql, rs -> {
            Long palId = rs.getLong("pal_id");
            partnerSkillByPalId.put(palId, new PartnerSkillSummaryView(rs.getString("title"), rs.getString("description"),
                    rs.getString("icon_url"), partnerSkillRanksByPalId.getOrDefault(palId, List.of())));
        });

        Map<Long, List<DropSummaryView>> dropsByPalId = new LinkedHashMap<>();
        final String dropsSql = """
                SELECT pd.pal_id, i.id AS item_id, i.slug AS item_slug, i.name AS item_name, i.icon_url AS item_icon_url,
                       pd.quantity_min, pd.quantity_max, pd.probability_percent, pd.level_label
                FROM tools_palworld.pal_drop pd
                JOIN tools_palworld.item i ON i.id = pd.item_id
                ORDER BY pd.pal_id, pd.sort_order
                """;
        jdbcTemplate.query(dropsSql, rs -> {
            dropsByPalId.computeIfAbsent(rs.getLong("pal_id"), id -> new ArrayList<>())
                    .add(new DropSummaryView(
                            rs.getLong("item_id"),
                            rs.getString("item_slug"),
                            rs.getString("item_name"),
                            rs.getString("item_icon_url"),
                            (Integer) rs.getObject("quantity_min"),
                            (Integer) rs.getObject("quantity_max"),
                            rs.getBigDecimal("probability_percent"),
                            rs.getString("level_label")));
        });

        Map<Long, List<VariantSummaryView>> variantsByPalId = new LinkedHashMap<>();
        final String variantsSql = """
                SELECT pal_id, id, slug, name, icon_url, role
                FROM tools_palworld.pal_variant
                ORDER BY pal_id, sort_order
                """;
        jdbcTemplate.query(variantsSql, rs -> {
            variantsByPalId.computeIfAbsent(rs.getLong("pal_id"), id -> new ArrayList<>())
                    .add(new VariantSummaryView(rs.getLong("id"), rs.getString("slug"), rs.getString("name"),
                            rs.getString("icon_url"), rs.getString("role")));
        });

        Map<Long, List<SpawnZoneSummaryView>> spawnZonesByPalId = new LinkedHashMap<>();
        final String spawnZonesSql = """
                SELECT pal_id, id, level_label, location_label, location_link
                FROM tools_palworld.pal_spawn_zone
                ORDER BY pal_id, sort_order
                """;
        jdbcTemplate.query(spawnZonesSql, rs -> {
            spawnZonesByPalId.computeIfAbsent(rs.getLong("pal_id"), id -> new ArrayList<>())
                    .add(new SpawnZoneSummaryView(rs.getLong("id"), rs.getString("level_label"),
                            rs.getString("location_label"), rs.getString("location_link")));
        });

        final String palsSql = """
                SELECT id, tribe, paldex_index, paldex_suffix, name, image_url, description, rarity, size, base_hp,
                       base_attack, base_defense, base_work_speed, base_support, food_amount, capture_rate_correct,
                       male_probability, combi_rank, gold_coin, egg_type, best_work_suitability_label
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
                    rs.getString("description"),
                    (Integer) rs.getObject("rarity"),
                    rs.getString("size"),
                    (Integer) rs.getObject("base_hp"),
                    (Integer) rs.getObject("base_attack"),
                    (Integer) rs.getObject("base_defense"),
                    (Integer) rs.getObject("base_work_speed"),
                    (Integer) rs.getObject("base_support"),
                    (Integer) rs.getObject("food_amount"),
                    rs.getBigDecimal("capture_rate_correct"),
                    rs.getBigDecimal("male_probability"),
                    (Integer) rs.getObject("combi_rank"),
                    (Integer) rs.getObject("gold_coin"),
                    rs.getString("egg_type"),
                    rs.getString("best_work_suitability_label"),
                    elementsByPalId.getOrDefault(id, List.of()),
                    workSuitabilitiesByPalId.getOrDefault(id, List.of()),
                    passiveSkillsByPalId.getOrDefault(id, List.of()),
                    activeSkillsByPalId.getOrDefault(id, List.of()),
                    partnerSkillByPalId.get(id),
                    dropsByPalId.getOrDefault(id, List.of()),
                    variantsByPalId.getOrDefault(id, List.of()),
                    spawnZonesByPalId.getOrDefault(id, List.of()));
        });
    }
}
