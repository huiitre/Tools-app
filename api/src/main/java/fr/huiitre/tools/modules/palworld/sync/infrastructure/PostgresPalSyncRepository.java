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
import fr.huiitre.tools.modules.palworld.sync.application.PalPartnerSkillRankSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalPassiveSkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalSpawnZoneSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalVariantSyncData;
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
            rs.getString("paldex_suffix"),
            rs.getString("name"),
            rs.getString("image_url"),
            rs.getString("description"),
            rs.getString("size"),
            (Integer) rs.getObject("rarity"),
            (Integer) rs.getObject("base_hp"),
            (Integer) rs.getObject("base_attack"),
            (Integer) rs.getObject("base_defense"),
            (Integer) rs.getObject("base_work_speed"),
            (Integer) rs.getObject("base_support"),
            (Integer) rs.getObject("food_amount"),
            (Integer) rs.getObject("run_speed"),
            (Integer) rs.getObject("ride_sprint_speed"),
            rs.getBigDecimal("capture_rate_correct"),
            rs.getBigDecimal("male_probability"),
            (Integer) rs.getObject("combi_rank"),
            (Integer) rs.getObject("gold_coin"),
            rs.getString("egg_type"),
            rs.getString("best_work_suitability_label"),
            (Integer) rs.getObject("food_gauge_filled"),
            (Integer) rs.getObject("food_gauge_empty"),
            rs.getString("food_gauge_icon_url"));

    @Override
    public List<PalRefView> findAll() {
        final String sql = """
                SELECT id, tribe, paldex_index, paldex_suffix, name, image_url, description, size, rarity,
                       base_hp, base_attack, base_defense, base_work_speed, base_support, food_amount, run_speed,
                       ride_sprint_speed, capture_rate_correct, male_probability, combi_rank, gold_coin, egg_type,
                       best_work_suitability_label, food_gauge_filled, food_gauge_empty, food_gauge_icon_url
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
                     ride_sprint_speed, capture_rate_correct, male_probability, combi_rank, gold_coin, egg_type,
                     best_work_suitability_label, food_gauge_filled, food_gauge_empty, food_gauge_icon_url)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                RETURNING id
                """;
        return jdbcTemplate.queryForObject(sql, Long.class,
                data.getTribe(), data.getPaldexIndex(), data.getPaldexSuffix(), data.getName(), data.getImageUrl(),
                data.getDescription(), data.getSize(), data.getRarity(), data.getBaseHp(), data.getBaseAttack(),
                data.getBaseDefense(), data.getBaseWorkSpeed(), data.getBaseSupport(), data.getFoodAmount(), data.getRunSpeed(),
                data.getRideSprintSpeed(), data.getCaptureRateCorrect(), data.getMaleProbability(), data.getCombiRank(),
                data.getGoldCoin(), data.getEggType(), data.getBestWorkSuitabilityLabel(), data.getFoodGaugeFilled(),
                data.getFoodGaugeEmpty(), data.getFoodGaugeIconUrl());
    }

    @Override
    public void update(Long id, PalSyncData data) {
        final String sql = """
                UPDATE tools_palworld.pal
                SET paldex_index = ?, paldex_suffix = ?, name = ?, image_url = ?, description = ?, size = ?,
                    rarity = ?, base_hp = ?, base_attack = ?, base_defense = ?, base_work_speed = ?, base_support = ?,
                    food_amount = ?, run_speed = ?, ride_sprint_speed = ?, capture_rate_correct = ?, male_probability = ?,
                    combi_rank = ?, gold_coin = ?, egg_type = ?, best_work_suitability_label = ?, food_gauge_filled = ?,
                    food_gauge_empty = ?, food_gauge_icon_url = ?, updated_at = now()
                WHERE id = ?
                """;
        jdbcTemplate.update(sql,
                data.getPaldexIndex(), data.getPaldexSuffix(), data.getName(), data.getImageUrl(), data.getDescription(),
                data.getSize(), data.getRarity(), data.getBaseHp(), data.getBaseAttack(), data.getBaseDefense(),
                data.getBaseWorkSpeed(), data.getBaseSupport(), data.getFoodAmount(), data.getRunSpeed(), data.getRideSprintSpeed(),
                data.getCaptureRateCorrect(), data.getMaleProbability(), data.getCombiRank(), data.getGoldCoin(),
                data.getEggType(), data.getBestWorkSuitabilityLabel(), data.getFoodGaugeFilled(), data.getFoodGaugeEmpty(),
                data.getFoodGaugeIconUrl(), id);
    }

    @Override
    public void delete(Long id) {
        jdbcTemplate.update("DELETE FROM tools_palworld.pal WHERE id = ?", id);
    }

    @Override
    public void upsertSource(Long palId, String slug, String sourceUrl, String rawPayloadJson, OffsetDateTime fetchedAt) {
        final String sql = """
                INSERT INTO tools_palworld.pal_source (pal_id, source_code, external_slug, external_url, raw_payload, fetched_at)
                VALUES (?, 'paldb_cc', ?, ?, ?::jsonb, ?)
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
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_partner_skill_rank");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_partner_skill");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_element");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_work_suitability");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_active_skill");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_passive_skill");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_drop");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_variant");
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_spawn_zone");
    }

    @Override
    public void saveElements(Long palId, PalSyncData data, Map<String, Long> elementIdByExternalCode) {
        final String sql = """
                INSERT INTO tools_palworld.pal_element (pal_id, element_id, sort_order)
                VALUES (?, ?, ?)
                ON CONFLICT (pal_id, element_id) DO NOTHING
                """;
        final String backfillIconSql = """
                UPDATE tools_palworld.element SET icon_url = ? WHERE id = ? AND icon_url IS NULL
                """;
        for (PalElementSyncData element : data.getElements()) {
            Long elementId = elementIdByExternalCode.get(element.getExternalCode());
            if (elementId == null) continue;
            jdbcTemplate.update(sql, palId, elementId, element.getSortOrder());
            if (element.getIconUrl() != null) {
                jdbcTemplate.update(backfillIconSql, element.getIconUrl(), elementId);
            }
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
        final String backfillIconSql = """
                UPDATE tools_palworld.work_suitability SET icon_url = ? WHERE id = ? AND icon_url IS NULL
                """;
        for (PalWorkSuitabilitySyncData ws : data.getWorkSuitabilities()) {
            Long workSuitabilityId = workSuitabilityIdBySlug.get(ws.getSlug());
            if (workSuitabilityId == null) continue;
            jdbcTemplate.update(sql, palId, workSuitabilityId, ws.getLevel(), ws.getMaxLevel(), ws.getStarSegments(),
                    ws.getEmptySegments(), ws.isPriority());
            if (ws.getIconUrl() != null) {
                jdbcTemplate.update(backfillIconSql, ws.getIconUrl(), workSuitabilityId);
            }
        }
    }

    @Override
    public void saveActiveSkills(Long palId, PalSyncData data, Map<String, Long> skillIdBySlug) {
        final String sql = """
                INSERT INTO tools_palworld.pal_active_skill (pal_id, skill_id, unlock_level, sort_order)
                VALUES (?, ?, ?, ?)
                ON CONFLICT (pal_id, skill_id, unlock_level) DO NOTHING
                """;
        for (PalActiveSkillSyncData skill : data.getActiveSkills()) {
            Long skillId = skillIdBySlug.get(skill.getSkillSlug());
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
    public void savePartnerSkill(Long palId, PalSyncData data) {
        if (data.getPartnerSkill() == null) return;

        final String sql = """
                INSERT INTO tools_palworld.pal_partner_skill (pal_id, title, description, icon_url)
                VALUES (?, ?, ?, ?)
                """;
        jdbcTemplate.update(sql, palId, data.getPartnerSkill().getTitle(), data.getPartnerSkill().getDescription(),
                data.getPartnerSkill().getIconUrl());

        final String rankSql = """
                INSERT INTO tools_palworld.pal_partner_skill_rank (pal_id, sort_order, level_label, detail)
                VALUES (?, ?, ?, ?)
                """;
        for (PalPartnerSkillRankSyncData rank : data.getPartnerSkill().getRanks()) {
            jdbcTemplate.update(rankSql, palId, rank.getSortOrder(), rank.getLevelLabel(), rank.getDetail());
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

    @Override
    public void saveVariants(Long palId, PalSyncData data) {
        final String sql = """
                INSERT INTO tools_palworld.pal_variant (pal_id, slug, name, icon_url, role, sort_order)
                VALUES (?, ?, ?, ?, ?, ?)
                ON CONFLICT (pal_id, slug, role) DO NOTHING
                """;
        for (PalVariantSyncData variant : data.getVariants()) {
            jdbcTemplate.update(sql, palId, variant.getSlug(), variant.getName(), variant.getIconUrl(),
                    variant.getRole(), variant.getSortOrder());
        }
    }

    @Override
    public void saveSpawnZones(Long palId, PalSyncData data) {
        final String sql = """
                INSERT INTO tools_palworld.pal_spawn_zone (pal_id, level_label, location_label, location_link, sort_order)
                VALUES (?, ?, ?, ?, ?)
                """;
        for (PalSpawnZoneSyncData zone : data.getSpawnZones()) {
            jdbcTemplate.update(sql, palId, zone.getLevelLabel(), zone.getLocationLabel(), zone.getLocationLink(),
                    zone.getSortOrder());
        }
    }
}
