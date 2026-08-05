package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.sql.Timestamp;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Map;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.palworld.sync.application.PalActiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalDropSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalElementSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalPassiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalWorkSuitabilitySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.PalRefView;

public class PostgresPalSyncRepository implements PalSyncRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresPalSyncRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<PalRefView> ROW_MAPPER = (rs, rowNum) -> new PalRefView(
            rs.getLong("id"),
            rs.getString("tribe"),
            (Integer) rs.getObject("paldex_index"),
            rs.getString("name"),
            rs.getString("size"),
            (Integer) rs.getObject("rarity"),
            (Integer) rs.getObject("base_hp"),
            (Integer) rs.getObject("base_attack"),
            (Integer) rs.getObject("base_defense"),
            (Integer) rs.getObject("base_work_speed"),
            (Integer) rs.getObject("base_support"),
            (Integer) rs.getObject("run_speed"),
            (Integer) rs.getObject("ride_sprint_speed"),
            rs.getBigDecimal("capture_rate_correct"),
            rs.getBigDecimal("male_probability"),
            (Integer) rs.getObject("combi_rank"),
            (Integer) rs.getObject("combi_duplicate_priority"),
            rs.getBoolean("ignore_combi"),
            (Integer) rs.getObject("price"),
            rs.getString("best_work_suitability_label"),
            rs.getString("image_url"),
            rs.getString("description"));

    @Override
    public List<PalRefView> findAll() {
        final String sql = """
                SELECT id, tribe, paldex_index, name, size, rarity, base_hp, base_attack, base_defense,
                       base_work_speed, base_support, run_speed, ride_sprint_speed, capture_rate_correct,
                       male_probability, combi_rank, combi_duplicate_priority, ignore_combi, price,
                       best_work_suitability_label, image_url, description
                FROM tools_palworld.pal
                """;
        return jdbcTemplate.query(sql, ROW_MAPPER);
    }

    @Override
    public Long save(PalSyncData data) {
        final String sql = """
                INSERT INTO tools_palworld.pal
                    (tribe, paldex_index, paldex_suffix, name, image_url, description, size, rarity,
                     base_hp, base_attack, base_defense, base_work_speed, base_support, food_amount, run_speed,
                     ride_sprint_speed, capture_rate_correct, male_probability, combi_rank, combi_duplicate_priority,
                     ignore_combi, price, best_work_suitability_label, food_gauge_filled, food_gauge_empty, food_gauge_icon_url)
                VALUES (?, ?, NULL, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, ?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, NULL, NULL)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class,
                data.getTribe(), data.getPaldexIndex(), data.getName(), data.getImageUrl(), data.getDescription(),
                data.getSize(), data.getRarity(), data.getBaseHp(), data.getBaseAttack(), data.getBaseDefense(),
                data.getBaseWorkSpeed(), data.getBaseSupport(), data.getRunSpeed(), data.getRideSprintSpeed(),
                data.getCaptureRateCorrect(), data.getMaleProbability(), data.getCombiRank(),
                data.getCombiDuplicatePriority(), data.isIgnoreCombi(), data.getPrice(),
                data.getBestWorkSuitabilityLabel());
    }

    @Override
    public void update(Long id, PalSyncData data) {
        final String sql = """
                UPDATE tools_palworld.pal
                SET paldex_index = ?, name = ?, image_url = ?, description = ?, size = ?, rarity = ?, base_hp = ?,
                    base_attack = ?, base_defense = ?, base_work_speed = ?, base_support = ?, run_speed = ?,
                    ride_sprint_speed = ?, capture_rate_correct = ?, male_probability = ?, combi_rank = ?,
                    combi_duplicate_priority = ?, ignore_combi = ?, price = ?,
                    best_work_suitability_label = ?, updated_at = now()
                WHERE id = ?
                """;
        jdbcTemplate.update(sql,
                data.getPaldexIndex(), data.getName(), data.getImageUrl(), data.getDescription(), data.getSize(),
                data.getRarity(), data.getBaseHp(), data.getBaseAttack(), data.getBaseDefense(), data.getBaseWorkSpeed(),
                data.getBaseSupport(), data.getRunSpeed(), data.getRideSprintSpeed(), data.getCaptureRateCorrect(),
                data.getMaleProbability(), data.getCombiRank(), data.getCombiDuplicatePriority(), data.isIgnoreCombi(),
                data.getPrice(), data.getBestWorkSuitabilityLabel(), id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_palworld.pal WHERE id = ?", id);
    }

    @Override
    public void upsertSource(Long palId, String slug, String sourceUrl, String rawPayloadJson, OffsetDateTime fetchedAt) {
        final String sql = """
                INSERT INTO tools_palworld.pal_source (pal_id, source_code, external_slug, external_url, raw_payload, fetched_at)
                VALUES (?, 'palworld_pak', ?, ?, ?::jsonb, ?)
                ON CONFLICT (pal_id, source_code)
                DO UPDATE SET external_slug = EXCLUDED.external_slug, external_url = EXCLUDED.external_url,
                    raw_payload = EXCLUDED.raw_payload, fetched_at = EXCLUDED.fetched_at
                """;
        jdbcTemplate.update(sql, palId, slug, sourceUrl, rawPayloadJson, Timestamp.from(fetchedAt.toInstant()));
    }

    @Override
    public Long findOrCreateItem(String slug, String name, String iconUrl) {
        final String sql = """
                INSERT INTO tools_palworld.item (slug, name, icon_url)
                VALUES (?, ?, ?)
                ON CONFLICT (slug) DO UPDATE SET name = EXCLUDED.name, icon_url = EXCLUDED.icon_url
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class, slug, name, iconUrl);
    }

    @Override
    public void deleteAllChildren() {
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_element");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_work_suitability");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_active_skill");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_passive_skill");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_drop");
    }

    @Override
    public void saveElements(Long palId, PalSyncData data, Map<String, Long> elementIdByName) {
        final String sql = """
                INSERT INTO tools_palworld.pal_element (pal_id, element_id, sort_order)
                VALUES (?, ?, ?)
                ON CONFLICT (pal_id, element_id) DO NOTHING
                """;
        for (PalElementSyncData element : data.getElements()) {
            Long elementId = elementIdByName.get(element.getElementName());
            if (elementId == null) continue;
            jdbcTemplate.update(sql, palId, elementId, element.getSortOrder());
        }
    }

    @Override
    public void saveWorkSuitabilities(Long palId, PalSyncData data, Map<String, Long> workSuitabilityIdBySlug) {
        final String sql = """
                INSERT INTO tools_palworld.pal_work_suitability
                    (pal_id, work_suitability_id, level, max_level, star_segments, empty_segments, is_priority)
                VALUES (?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT (pal_id, work_suitability_id) DO NOTHING
                """;
        for (PalWorkSuitabilitySyncData ws : data.getWorkSuitabilities()) {
            Long workSuitabilityId = workSuitabilityIdBySlug.get(ws.getSlug());
            if (workSuitabilityId == null) continue;
            // star_segments/empty_segments : pas de donnée pak (jauge visuelle scrapée par l'ancien scraper,
            // cf. V2.53.0), toujours null ici. max_level/is_priority sont calculés — cf.
            // WorkSuitabilityMaxLevelCalculator et PalworldLocalPalDataProvider.workSuitabilities().
            jdbcTemplate.update(sql, palId, workSuitabilityId, ws.getLevel(), ws.getMaxLevel(), ws.getStarSegments(),
                    ws.getEmptySegments(), ws.isPriority());
        }
    }

    @Override
    public void saveActiveSkills(Long palId, PalSyncData data, Map<String, Long> skillIdByName) {
        final String sql = """
                INSERT INTO tools_palworld.pal_active_skill (pal_id, skill_id, unlock_level, sort_order)
                VALUES (?, ?, ?, ?)
                ON CONFLICT (pal_id, skill_id, unlock_level) DO NOTHING
                """;
        for (PalActiveSkillSyncData skill : data.getActiveSkills()) {
            Long skillId = skillIdByName.get(skill.getSkillName());
            if (skillId == null) continue;
            jdbcTemplate.update(sql, palId, skillId, skill.getUnlockLevel(), skill.getSortOrder());
        }
    }

    @Override
    public void savePassiveSkills(Long palId, PalSyncData data) {
        final String sql = """
                INSERT INTO tools_palworld.pal_passive_skill (pal_id, name, tooltip, rank_icon_url)
                VALUES (?, ?, ?, ?)
                """;
        for (PalPassiveSkillSyncData passive : data.getPassiveSkills()) {
            jdbcTemplate.update(sql, palId, passive.getName(), passive.getTooltip(), passive.getRankIconUrl());
        }
    }

    @Override
    public void saveDrops(Long palId, PalSyncData data) {
        final String sql = """
                INSERT INTO tools_palworld.pal_drop
                    (pal_id, item_id, quantity_min, quantity_max, probability_percent, level_label, sort_order)
                VALUES (?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT (pal_id, item_id, sort_order) DO NOTHING
                """;
        for (PalDropSyncData drop : data.getDrops()) {
            if (drop.getItemSlug() == null) continue;
            Long itemId = findOrCreateItem(drop.getItemSlug(), drop.getItemName(), drop.getItemIconUrl());
            jdbcTemplate.update(sql, palId, itemId, drop.getQuantityMin(), drop.getQuantityMax(),
                    drop.getProbabilityPercent(), drop.getLevelLabel(), drop.getSortOrder());
        }
    }
}
