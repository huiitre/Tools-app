package fr.huiitre.tools.modules.palworld.serverdata.infrastructure;

import java.sql.Array;
import java.sql.Timestamp;
import java.sql.SQLException;
import java.util.Map;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.serverdata.application.BaseSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.GuildSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.PalInstanceSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.PlayerSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.ServerSnapshotSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.ServerDataRepository;

public class PostgresServerDataRepository implements ServerDataRepository {

    private final JdbcTemplate jdbcTemplate;

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
        Timestamp extractedAt = Timestamp.from(data.getExtractedAt().toInstant());

        for (GuildSyncData guild : data.getGuilds()) {
            upsertGuild(guild, extractedAt);
            for (PlayerSyncData player : guild.getPlayers()) {
                upsertPlayer(player, guild.getGuildId(), extractedAt);
            }
        }

        for (BaseSyncData base : data.getBases()) {
            upsertBase(base, extractedAt);
        }

        markMissingPalsAsNotPresent(extractedAt);
        for (PalInstanceSyncData pal : data.getPalInstances()) {
            Long palId = resolvePalId(pal, palIdByTribeUpper);
            upsertPalInstance(pal, palId, extractedAt);
            insertSnapshot(pal, extractedAt);
        }

        insertImportTrackingRow(fileName, data, extractedAt);
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

    private void upsertPalInstance(PalInstanceSyncData pal, Long palId, Timestamp extractedAt) {
        final String sql = """
                INSERT INTO tools_palworld.pal_instance
                    (instance_id, character_id, pal_id, is_alpha, owner_player_uid, base_id, storage_location, container_id,
                     gender, favorite_index, passive_skill_ids, is_present, level, exp, full_stomach,
                     is_sick, workable_type, task_id, work_state, current_work_amount, required_work_amount,
                     first_seen_at, last_seen_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT (instance_id) DO UPDATE SET
                    character_id = EXCLUDED.character_id, pal_id = EXCLUDED.pal_id, is_alpha = EXCLUDED.is_alpha,
                    owner_player_uid = EXCLUDED.owner_player_uid, base_id = EXCLUDED.base_id,
                    storage_location = EXCLUDED.storage_location, container_id = EXCLUDED.container_id,
                    gender = EXCLUDED.gender, favorite_index = EXCLUDED.favorite_index,
                    passive_skill_ids = EXCLUDED.passive_skill_ids, is_present = TRUE, level = EXCLUDED.level,
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
                preparedStatement.setBoolean(12, true);
                preparedStatement.setObject(13, pal.getLevel());
                preparedStatement.setObject(14, pal.getExp());
                preparedStatement.setBigDecimal(15, pal.getFullStomach());
                preparedStatement.setObject(16, pal.getIsSick());
                preparedStatement.setString(17, pal.getWorkableType());
                preparedStatement.setString(18, pal.getTaskId());
                preparedStatement.setObject(19, pal.getWorkState());
                preparedStatement.setBigDecimal(20, pal.getCurrentWorkAmount());
                preparedStatement.setBigDecimal(21, pal.getRequiredWorkAmount());
                preparedStatement.setTimestamp(22, extractedAt);
                preparedStatement.setTimestamp(23, extractedAt);
            } catch (SQLException e) {
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
