package fr.huiitre.tools.modules.palworld.serverdata.infrastructure;

import java.io.IOException;
import java.math.BigDecimal;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;
import java.util.stream.Collectors;
import java.util.stream.Stream;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.serverdata.application.BaseSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.GuildSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.PalInstanceSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.PlayerSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.ServerSnapshotSyncData;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.PendingSnapshotFile;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.ServerSnapshotFilePort;

import jakarta.annotation.PostConstruct;

@Component
public class PalworldServerSnapshotFileAdapter implements ServerSnapshotFilePort {

    private static final Logger log = LoggerFactory.getLogger(PalworldServerSnapshotFileAdapter.class);

    private final ObjectMapper objectMapper = new ObjectMapper();

    @Value("${palworld.server-data.path}")
    private Path snapshotDirectory;

    @Value("${palworld.server-data.min-file-age-seconds:60}")
    private long minFileAgeSeconds;

    private Path archiveDirectory;

    @PostConstruct
    void init() {
        if (snapshotDirectory == null) {
            throw new IllegalStateException("palworld.server-data.path is not configured");
        }
        archiveDirectory = snapshotDirectory.resolve("imported");
        try {
            Files.createDirectories(archiveDirectory);
        } catch (IOException e) {
            log.warn("Palworld server-data directory unavailable ({}), feature will be inactive until it is: {}",
                    archiveDirectory, e.getMessage());
        }
    }

    @Override
    public List<PendingSnapshotFile> listPendingFiles() {
        Instant cutoff = Instant.now().minusSeconds(minFileAgeSeconds);

        try (Stream<Path> files = Files.list(snapshotDirectory)) {
            return files
                    .filter(path -> !Files.isDirectory(path))
                    .filter(path -> path.getFileName().toString().endsWith(".json"))
                    .filter(path -> isOldEnough(path, cutoff))
                    .sorted(Comparator.comparing(path -> path.getFileName().toString()))
                    .map(path -> new PendingSnapshotFile(path, path.getFileName().toString()))
                    .collect(Collectors.toList());
        } catch (IOException e) {
            throw new IllegalStateException("Unable to list Palworld server-data snapshot files in " + snapshotDirectory, e);
        }
    }

    private boolean isOldEnough(Path path, Instant cutoff) {
        try {
            return Files.getLastModifiedTime(path).toInstant().isBefore(cutoff);
        } catch (IOException e) {
            return false;
        }
    }

    @Override
    public ServerSnapshotSyncData readAndParse(Path file) {
        try {
            JsonNode root = objectMapper.readTree(file.toFile());

            OffsetDateTime extractedAt = Instant.parse(root.path("extracted_at").asText()).atOffset(ZoneOffset.UTC);
            List<GuildSyncData> guilds = parseGuilds(root.path("guilds"));
            List<BaseSyncData> bases = parseBases(root.path("bases"));
            List<PalInstanceSyncData> pals = parsePalInstances(root.path("pals"));

            return new ServerSnapshotSyncData(extractedAt, guilds, bases, pals);
        } catch (IOException e) {
            throw new IllegalStateException("Unable to parse Palworld server-data snapshot file: " + file, e);
        }
    }

    @Override
    public void archive(Path file) {
        // The scheduler and the manual endpoint can observe the same file at
        // the same time. If the other execution already moved it, archiving is
        // already complete and must remain idempotent.
        if (!Files.exists(file)) return;
        try {
            Files.move(file, archiveDirectory.resolve(file.getFileName()), StandardCopyOption.REPLACE_EXISTING);
        } catch (IOException e) {
            if (!Files.exists(file) && Files.exists(archiveDirectory.resolve(file.getFileName()))) return;
            throw new IllegalStateException("Unable to archive Palworld server-data snapshot file: " + file, e);
        }
    }

    private List<GuildSyncData> parseGuilds(JsonNode node) {
        List<GuildSyncData> result = new ArrayList<>();
        for (JsonNode guild : node) {
            List<PlayerSyncData> players = new ArrayList<>();
            for (JsonNode player : guild.path("players")) {
                players.add(new PlayerSyncData(
                        parseUuid(player.path("player_uid").asText(null)),
                        player.path("player_name").asText(null),
                        player.path("last_online_real_time").isMissingNode() ? null : player.path("last_online_real_time").asLong()));
            }

            result.add(new GuildSyncData(
                    parseUuid(guild.path("guild_id").asText(null)),
                    guild.path("guild_name").asText(null),
                    players));
        }
        return result;
    }

    private List<BaseSyncData> parseBases(JsonNode node) {
        List<BaseSyncData> result = new ArrayList<>();
        for (JsonNode base : node) {
            result.add(new BaseSyncData(
                    parseUuid(base.path("base_id").asText(null)),
                    parseUuid(base.path("guild_id").asText(null))));
        }
        return result;
    }

    private List<PalInstanceSyncData> parsePalInstances(JsonNode node) {
        List<PalInstanceSyncData> result = new ArrayList<>();
        for (JsonNode pal : node) {
            JsonNode work = pal.path("work");
            boolean hasWork = work.isObject();

            result.add(new PalInstanceSyncData(
                    parseUuid(pal.path("instance_id").asText(null)),
                    pal.path("character_id").asText(null),
                    parseUuid(pal.path("owner_player_uid").asText(null)),
                    parseUuid(pal.path("base_id").asText(null)),
                    pal.path("storage_location").asText("base"),
                    parseUuid(pal.path("container_id").asText(null)),
                    pal.path("gender").asText(null),
                    parseInt(pal.path("favorite_index")),
                    parseStringList(pal.path("passive_skill_ids")),
                    parseInt(pal.path("rank")),
                    parseInt(pal.path("iv_hp")), parseInt(pal.path("iv_attack")), parseInt(pal.path("iv_defense")),
                    parseDecimal(pal.path("current_hp")),
                    parseInt(pal.path("base_stats").path("hp")),
                    parseInt(pal.path("base_stats").path("meleeAttack")),
                    parseInt(pal.path("base_stats").path("shotAttack")),
                    parseInt(pal.path("base_stats").path("defense")),
                    parseInt(pal.path("base_stats").path("support")),
                    parseInt(pal.path("base_stats").path("craftSpeed")),
                    parseIntMap(pal.path("base_work_suitability")),
                    parseIntMap(pal.path("work_suitability_add_ranks")),
                    parseInt(pal.path("level")),
                    parseInt(pal.path("exp")),
                    parseDecimal(pal.path("full_stomach")),
                    parseBoolean(pal.path("is_sick")),
                    hasWork ? work.path("workable_type").asText(null) : null,
                    hasWork ? work.path("task_id").asText(null) : null,
                    hasWork ? parseInt(work.path("state")) : null,
                    hasWork ? parseDecimal(work.path("current_work_amount")) : null,
                    hasWork ? parseDecimal(work.path("required_work_amount")) : null));
        }
        return result;
    }

    private Map<String, Integer> parseIntMap(JsonNode node) {
        Map<String, Integer> result = new LinkedHashMap<>();
        if (!node.isObject()) return result;
        node.fields().forEachRemaining(entry -> {
            Integer value = parseInt(entry.getValue());
            if (value != null) result.put(entry.getKey(), value);
        });
        return result;
    }

    private UUID parseUuid(String raw) {
        return (raw != null && !raw.isBlank()) ? UUID.fromString(raw) : null;
    }

    private BigDecimal parseDecimal(JsonNode node) {
        return isAbsent(node) ? null : BigDecimal.valueOf(node.asDouble());
    }

    private Integer parseInt(JsonNode node) {
        return isAbsent(node) ? null : node.asInt();
    }

    private Boolean parseBoolean(JsonNode node) {
        return isAbsent(node) ? null : node.asBoolean();
    }

    private List<String> parseStringList(JsonNode node) {
        List<String> values = new ArrayList<>();
        if (!node.isArray()) return values;
        for (JsonNode value : node) {
            values.add(value.asText());
        }
        return values;
    }

    private boolean isAbsent(JsonNode node) {
        return node == null || node.isMissingNode() || node.isNull();
    }
}
