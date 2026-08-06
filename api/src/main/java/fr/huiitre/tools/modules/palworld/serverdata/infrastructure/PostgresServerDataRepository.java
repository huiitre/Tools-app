package fr.huiitre.tools.modules.palworld.serverdata.infrastructure;

import java.sql.Array;
import java.sql.Timestamp;
import java.sql.SQLException;
import java.sql.Types;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.serverdata.application.BaseSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.GuildSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.PalInstanceSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.PlayerSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.ServerSnapshotSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.ServerDataRepository;

public class PostgresServerDataRepository implements ServerDataRepository {

    private static final Logger log = LoggerFactory.getLogger(PostgresServerDataRepository.class);

    private final ObjectMapper objectMapper = new ObjectMapper();

    private final JdbcTemplate jdbcTemplate;
    private static final int PAL_BATCH_SIZE = 500;
    private static final String PAL_INSTANCE_UPSERT_SQL = """
            INSERT INTO tools_palworld.pal_instance
                (instance_id, character_id, pal_id, is_alpha, owner_player_uid, base_id, storage_location, container_id,
                 gender, favorite_index, passive_skill_ids, rank, iv_hp, iv_attack, iv_defense, current_hp,
                 base_hp, base_melee_attack, base_shot_attack, base_defense, base_support, base_craft_speed,
                 base_work_suitability, work_suitability_add_ranks, is_present, level, exp, full_stomach,
                 is_sick, workable_type, task_id, work_state, current_work_amount, required_work_amount,
                 first_seen_at, last_seen_at)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT (instance_id) DO UPDATE SET
                character_id = EXCLUDED.character_id, pal_id = EXCLUDED.pal_id, is_alpha = EXCLUDED.is_alpha,
                owner_player_uid = EXCLUDED.owner_player_uid, base_id = EXCLUDED.base_id,
                storage_location = EXCLUDED.storage_location, container_id = EXCLUDED.container_id,
                gender = EXCLUDED.gender, favorite_index = EXCLUDED.favorite_index,
                passive_skill_ids = EXCLUDED.passive_skill_ids, rank = EXCLUDED.rank, iv_hp = EXCLUDED.iv_hp,
                iv_attack = EXCLUDED.iv_attack, iv_defense = EXCLUDED.iv_defense, current_hp = EXCLUDED.current_hp,
                base_hp = EXCLUDED.base_hp, base_melee_attack = EXCLUDED.base_melee_attack,
                base_shot_attack = EXCLUDED.base_shot_attack, base_defense = EXCLUDED.base_defense,
                base_support = EXCLUDED.base_support, base_craft_speed = EXCLUDED.base_craft_speed,
                base_work_suitability = EXCLUDED.base_work_suitability,
                work_suitability_add_ranks = EXCLUDED.work_suitability_add_ranks,
                is_present = TRUE, level = EXCLUDED.level, exp = EXCLUDED.exp, full_stomach = EXCLUDED.full_stomach,
                is_sick = EXCLUDED.is_sick, workable_type = EXCLUDED.workable_type, task_id = EXCLUDED.task_id,
                work_state = EXCLUDED.work_state, current_work_amount = EXCLUDED.current_work_amount,
                required_work_amount = EXCLUDED.required_work_amount, last_seen_at = EXCLUDED.last_seen_at
            WHERE tools_palworld.pal_instance.last_seen_at <= EXCLUDED.last_seen_at
            """;
    private static final String PAL_SNAPSHOT_INSERT_SQL = """
            INSERT INTO tools_palworld.pal_instance_snapshot
                (instance_id, captured_at, base_id, level, exp, full_stomach, is_sick, workable_type, task_id,
                 work_state, current_work_amount, required_work_amount)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT (instance_id, captured_at) DO NOTHING
            """;

    public PostgresServerDataRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public boolean isFileAlreadyImported(String fileName) {
        Integer count = jdbcTemplate.queryForObject(
                "SELECT COUNT(*) FROM tools_palworld.server_snapshot_import WHERE file_name = ?",
                Integer.class, fileName);
        return count != null && count > 0;
    }

    @Override
    public void importSnapshot(String fileName, ServerSnapshotSyncData data, Map<String, Long> palIdByTribeUpper) {
        long startedAt = System.nanoTime();
        Timestamp extractedAt = Timestamp.from(data.getExtractedAt().toInstant());

        long guildsStartedAt = System.nanoTime();
        int playerCount = 0;
        for (GuildSyncData guild : data.getGuilds()) {
            upsertGuild(guild, extractedAt);
            for (PlayerSyncData player : guild.getPlayers()) {
                upsertPlayer(player, guild.getGuildId(), extractedAt);
                playerCount++;
            }
        }
        log.info("Palworld import metrics file={} phase=guilds guilds={} players={} durationMs={}", fileName,
                data.getGuilds().size(), playerCount, elapsedMs(guildsStartedAt));

        long basesStartedAt = System.nanoTime();
        for (BaseSyncData base : data.getBases()) {
            upsertBase(base, extractedAt);
        }
        log.info("Palworld import metrics file={} phase=bases bases={} durationMs={}", fileName,
                data.getBases().size(), elapsedMs(basesStartedAt));

        long missingStartedAt = System.nanoTime();
        markMissingPalsAsNotPresent(extractedAt);
        log.info("Palworld import metrics file={} phase=mark-missing durationMs={}", fileName, elapsedMs(missingStartedAt));

        long palsStartedAt = System.nanoTime();
        batchUpsertPalInstances(data.getPalInstances(), palIdByTribeUpper, extractedAt);
        batchInsertSnapshots(data.getPalInstances(), extractedAt);
        long palsDurationMs = elapsedMs(palsStartedAt);
        log.info("Palworld import metrics file={} phase=pals pals={} durationMs={} avgMsPerPal={}", fileName,
                data.getPalInstances().size(), palsDurationMs,
                data.getPalInstances().isEmpty() ? 0 : (double) palsDurationMs / data.getPalInstances().size());

        long trackingStartedAt = System.nanoTime();
        insertImportTrackingRow(fileName, data, extractedAt);
        log.info("Palworld import metrics file={} phase=tracking durationMs={} totalDbMs={}", fileName,
                elapsedMs(trackingStartedAt), elapsedMs(startedAt));
    }

    private void batchUpsertPalInstances(List<PalInstanceSyncData> pals, Map<String, Long> palIds, Timestamp extractedAt) {
        jdbcTemplate.batchUpdate(PAL_INSTANCE_UPSERT_SQL, pals, PAL_BATCH_SIZE,
                (statement, pal) -> bindPalInstance(statement, statement.getConnection(), pal,
                        resolvePalId(pal, palIds), extractedAt));
    }

    private void batchInsertSnapshots(List<PalInstanceSyncData> pals, Timestamp extractedAt) {
        List<PalInstanceSyncData> snapshotPals = pals.stream()
                .filter(pal -> pal.getBaseId() != null)
                .toList();
        jdbcTemplate.batchUpdate(PAL_SNAPSHOT_INSERT_SQL, snapshotPals, PAL_BATCH_SIZE, (statement, pal) -> {
            statement.setObject(1, pal.getInstanceId()); statement.setTimestamp(2, extractedAt);
            statement.setObject(3, pal.getBaseId()); statement.setObject(4, pal.getLevel());
            statement.setObject(5, pal.getExp()); statement.setBigDecimal(6, pal.getFullStomach());
            statement.setObject(7, pal.getIsSick()); statement.setString(8, pal.getWorkableType());
            statement.setString(9, pal.getTaskId()); statement.setObject(10, pal.getWorkState());
            statement.setBigDecimal(11, pal.getCurrentWorkAmount()); statement.setBigDecimal(12, pal.getRequiredWorkAmount());
        });
    }

    private long elapsedMs(long startedAt) {
        return (System.nanoTime() - startedAt) / 1_000_000;
    }

    private void markMissingPalsAsNotPresent(Timestamp extractedAt) {
        jdbcTemplate.update("""
                UPDATE tools_palworld.pal_instance
                SET is_present = FALSE
                WHERE is_present = TRUE AND last_seen_at < ?
                """, extractedAt);
    }

    private Long resolvePalId(PalInstanceSyncData pal, Map<String, Long> palIdByTribeUpper) {
        String characterId = pal.characterIdWithoutBossPrefix();
        return characterId != null ? palIdByTribeUpper.get(characterId.toUpperCase()) : null;
    }

    private void upsertGuild(GuildSyncData guild, Timestamp extractedAt) {
        final String sql = """
                INSERT INTO tools_palworld.guild (guild_id, name, first_seen_at, last_seen_at)
                VALUES (?, ?, ?, ?)
                ON CONFLICT (guild_id) DO UPDATE SET name = EXCLUDED.name, last_seen_at = EXCLUDED.last_seen_at
                WHERE tools_palworld.guild.last_seen_at <= EXCLUDED.last_seen_at
                """;
        jdbcTemplate.update(sql, guild.getGuildId(), guild.getName(), extractedAt, extractedAt);
    }

    private void upsertPlayer(PlayerSyncData player, UUID guildId, Timestamp extractedAt) {
        final String sql = """
                INSERT INTO tools_palworld.player (player_uid, name, guild_id, last_online_real_time, first_seen_at, last_seen_at)
                VALUES (?, ?, ?, ?, ?, ?)
                ON CONFLICT (player_uid) DO UPDATE SET name = EXCLUDED.name, guild_id = EXCLUDED.guild_id,
                    last_online_real_time = EXCLUDED.last_online_real_time, last_seen_at = EXCLUDED.last_seen_at
                WHERE tools_palworld.player.last_seen_at <= EXCLUDED.last_seen_at
                """;
        jdbcTemplate.update(sql, player.getPlayerUid(), player.getName(), guildId, player.getLastOnlineRealTime(),
                extractedAt, extractedAt);
    }

    private void upsertBase(BaseSyncData base, Timestamp extractedAt) {
        final String sql = """
                INSERT INTO tools_palworld.base (base_id, guild_id, first_seen_at, last_seen_at)
                VALUES (?, ?, ?, ?)
                ON CONFLICT (base_id) DO UPDATE SET guild_id = EXCLUDED.guild_id, last_seen_at = EXCLUDED.last_seen_at
                WHERE tools_palworld.base.last_seen_at <= EXCLUDED.last_seen_at
                """;
        jdbcTemplate.update(sql, base.getBaseId(), base.getGuildId(), extractedAt, extractedAt);
    }

    private void bindPalInstance(PreparedStatement statement, Connection connection, PalInstanceSyncData pal,
            Long palId, Timestamp extractedAt) throws SQLException {
        try {
            statement.setObject(1, pal.getInstanceId()); statement.setString(2, pal.getCharacterId());
            statement.setObject(3, palId); statement.setBoolean(4, pal.isAlpha());
            statement.setObject(5, pal.getOwnerPlayerUid()); statement.setObject(6, pal.getBaseId());
            statement.setString(7, pal.getStorageLocation()); statement.setObject(8, pal.getContainerId());
            statement.setString(9, pal.getGender()); statement.setObject(10, pal.getFavoriteIndex());
            Array passiveSkillIds = connection.createArrayOf("text", pal.getPassiveSkillIds().toArray(String[]::new));
            statement.setArray(11, passiveSkillIds); statement.setObject(12, pal.getRank());
            statement.setObject(13, pal.getIvHp()); statement.setObject(14, pal.getIvAttack());
            statement.setObject(15, pal.getIvDefense()); statement.setBigDecimal(16, pal.getCurrentHp());
            statement.setObject(17, pal.getBaseHp()); statement.setObject(18, pal.getBaseMeleeAttack());
            statement.setObject(19, pal.getBaseShotAttack()); statement.setObject(20, pal.getBaseDefense());
            statement.setObject(21, pal.getBaseSupport()); statement.setObject(22, pal.getBaseCraftSpeed());
            statement.setObject(23, objectMapper.writeValueAsString(pal.getBaseWorkSuitability()), Types.OTHER);
            statement.setObject(24, objectMapper.writeValueAsString(pal.getWorkSuitabilityAddRanks()), Types.OTHER);
            statement.setBoolean(25, true); statement.setObject(26, pal.getLevel());
            statement.setObject(27, pal.getExp()); statement.setBigDecimal(28, pal.getFullStomach());
            statement.setObject(29, pal.getIsSick()); statement.setString(30, pal.getWorkableType());
            statement.setString(31, pal.getTaskId()); statement.setObject(32, pal.getWorkState());
            statement.setBigDecimal(33, pal.getCurrentWorkAmount()); statement.setBigDecimal(34, pal.getRequiredWorkAmount());
            statement.setTimestamp(35, extractedAt); statement.setTimestamp(36, extractedAt);
        } catch (Exception e) {
            if (e instanceof SQLException sqlException) throw sqlException;
            throw new SQLException("Unable to bind Palworld Pal batch row", e);
        }
    }

    private void upsertPalInstance(PalInstanceSyncData pal, Long palId, Timestamp extractedAt) {
        final String sql = """
                INSERT INTO tools_palworld.pal_instance
                    (instance_id, character_id, pal_id, is_alpha, owner_player_uid, base_id, storage_location, container_id,
                     gender, favorite_index, passive_skill_ids, rank, iv_hp, iv_attack, iv_defense, current_hp,
                     base_hp, base_melee_attack, base_shot_attack, base_defense, base_support, base_craft_speed,
                     base_work_suitability, work_suitability_add_ranks, is_present, level, exp, full_stomach,
                     is_sick, workable_type, task_id, work_state, current_work_amount, required_work_amount,
                     first_seen_at, last_seen_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT (instance_id) DO UPDATE SET
                    character_id = EXCLUDED.character_id, pal_id = EXCLUDED.pal_id, is_alpha = EXCLUDED.is_alpha,
                    owner_player_uid = EXCLUDED.owner_player_uid, base_id = EXCLUDED.base_id,
                    storage_location = EXCLUDED.storage_location, container_id = EXCLUDED.container_id,
                    gender = EXCLUDED.gender, favorite_index = EXCLUDED.favorite_index,
                    passive_skill_ids = EXCLUDED.passive_skill_ids, rank = EXCLUDED.rank, iv_hp = EXCLUDED.iv_hp,
                    iv_attack = EXCLUDED.iv_attack, iv_defense = EXCLUDED.iv_defense, current_hp = EXCLUDED.current_hp,
                    base_hp = EXCLUDED.base_hp, base_melee_attack = EXCLUDED.base_melee_attack,
                    base_shot_attack = EXCLUDED.base_shot_attack, base_defense = EXCLUDED.base_defense,
                    base_support = EXCLUDED.base_support, base_craft_speed = EXCLUDED.base_craft_speed,
                    base_work_suitability = EXCLUDED.base_work_suitability,
                    work_suitability_add_ranks = EXCLUDED.work_suitability_add_ranks,
                    is_present = TRUE, level = EXCLUDED.level,
                    exp = EXCLUDED.exp, full_stomach = EXCLUDED.full_stomach, is_sick = EXCLUDED.is_sick,
                    workable_type = EXCLUDED.workable_type, task_id = EXCLUDED.task_id,
                    work_state = EXCLUDED.work_state, current_work_amount = EXCLUDED.current_work_amount,
                    required_work_amount = EXCLUDED.required_work_amount, last_seen_at = EXCLUDED.last_seen_at
                WHERE tools_palworld.pal_instance.last_seen_at <= EXCLUDED.last_seen_at
                """;
        jdbcTemplate.update(connection -> {
            var preparedStatement = connection.prepareStatement(sql);
            try {
                preparedStatement.setObject(1, pal.getInstanceId());
                preparedStatement.setString(2, pal.getCharacterId());
                preparedStatement.setObject(3, palId);
                preparedStatement.setBoolean(4, pal.isAlpha());
                preparedStatement.setObject(5, pal.getOwnerPlayerUid());
                preparedStatement.setObject(6, pal.getBaseId());
                preparedStatement.setString(7, pal.getStorageLocation());
                preparedStatement.setObject(8, pal.getContainerId());
                preparedStatement.setString(9, pal.getGender());
                preparedStatement.setObject(10, pal.getFavoriteIndex());
                Array passiveSkillIds = connection.createArrayOf("text", pal.getPassiveSkillIds().toArray(String[]::new));
                preparedStatement.setArray(11, passiveSkillIds);
                preparedStatement.setObject(12, pal.getRank()); preparedStatement.setObject(13, pal.getIvHp());
                preparedStatement.setObject(14, pal.getIvAttack()); preparedStatement.setObject(15, pal.getIvDefense());
                preparedStatement.setBigDecimal(16, pal.getCurrentHp());
                preparedStatement.setObject(17, pal.getBaseHp()); preparedStatement.setObject(18, pal.getBaseMeleeAttack());
                preparedStatement.setObject(19, pal.getBaseShotAttack()); preparedStatement.setObject(20, pal.getBaseDefense());
                preparedStatement.setObject(21, pal.getBaseSupport()); preparedStatement.setObject(22, pal.getBaseCraftSpeed());
                preparedStatement.setObject(23, objectMapper.writeValueAsString(pal.getBaseWorkSuitability()), Types.OTHER);
                preparedStatement.setObject(24, objectMapper.writeValueAsString(pal.getWorkSuitabilityAddRanks()), Types.OTHER);
                preparedStatement.setBoolean(25, true); preparedStatement.setObject(26, pal.getLevel());
                preparedStatement.setObject(27, pal.getExp()); preparedStatement.setBigDecimal(28, pal.getFullStomach());
                preparedStatement.setObject(29, pal.getIsSick()); preparedStatement.setString(30, pal.getWorkableType());
                preparedStatement.setString(31, pal.getTaskId()); preparedStatement.setObject(32, pal.getWorkState());
                preparedStatement.setBigDecimal(33, pal.getCurrentWorkAmount()); preparedStatement.setBigDecimal(34, pal.getRequiredWorkAmount());
                preparedStatement.setTimestamp(35, extractedAt); preparedStatement.setTimestamp(36, extractedAt);
            } catch (Exception e) {
                throw new IllegalStateException("Unable to bind Palworld server Pal instance", e);
            }
            return preparedStatement;
        });
    }

    private void insertSnapshot(PalInstanceSyncData pal, Timestamp extractedAt) {
        if (pal.getBaseId() == null) return;
        final String sql = """
                INSERT INTO tools_palworld.pal_instance_snapshot
                    (instance_id, captured_at, base_id, level, exp, full_stomach, is_sick, workable_type, task_id,
                     work_state, current_work_amount, required_work_amount)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT (instance_id, captured_at) DO NOTHING
                """;
        jdbcTemplate.update(sql,
                pal.getInstanceId(), extractedAt, pal.getBaseId(), pal.getLevel(), pal.getExp(), pal.getFullStomach(),
                pal.getIsSick(), pal.getWorkableType(), pal.getTaskId(), pal.getWorkState(), pal.getCurrentWorkAmount(),
                pal.getRequiredWorkAmount());
    }

    private void insertImportTrackingRow(String fileName, ServerSnapshotSyncData data, Timestamp extractedAt) {
        final String sql = """
                INSERT INTO tools_palworld.server_snapshot_import (file_name, extracted_at, guild_count, base_count, pal_count)
                VALUES (?, ?, ?, ?, ?)
                """;
        jdbcTemplate.update(sql, fileName, extractedAt, data.getGuilds().size(), data.getBases().size(),
                data.getPalInstances().size());
    }
}
