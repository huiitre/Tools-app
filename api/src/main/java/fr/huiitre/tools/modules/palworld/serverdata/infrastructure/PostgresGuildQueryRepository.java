package fr.huiitre.tools.modules.palworld.serverdata.infrastructure;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.serverdata.application.ports.GuildQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.BaseSummaryView;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.GuildSummaryView;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.PlayerSummaryView;

public class PostgresGuildQueryRepository implements GuildQueryRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresGuildQueryRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public List<GuildSummaryView> findAllWithMembersAndBases() {
        Map<UUID, String> guildNameById = new LinkedHashMap<>();
        jdbcTemplate.query("SELECT guild_id, name FROM tools_palworld.guild ORDER BY name", rs -> {
            guildNameById.put(rs.getObject("guild_id", UUID.class), rs.getString("name"));
        });

        Map<UUID, List<PlayerSummaryView>> playersByGuildId = new LinkedHashMap<>();
        final String playersSql = """
                SELECT guild_id, player_uid, name, last_online_real_time
                FROM tools_palworld.player
                WHERE guild_id IS NOT NULL
                ORDER BY name
                """;
        jdbcTemplate.query(playersSql, rs -> {
            UUID guildId = rs.getObject("guild_id", UUID.class);
            playersByGuildId.computeIfAbsent(guildId, id -> new ArrayList<>()).add(new PlayerSummaryView(
                    rs.getObject("player_uid", UUID.class),
                    rs.getString("name"),
                    (Long) rs.getObject("last_online_real_time")));
        });

        Map<UUID, List<BaseSummaryView>> basesByGuildId = new LinkedHashMap<>();
        final String basesSql = """
                SELECT b.guild_id, b.base_id, count(pi.instance_id) AS pal_count
                FROM tools_palworld.base b
                LEFT JOIN tools_palworld.pal_instance pi ON pi.base_id = b.base_id AND pi.is_present = TRUE
                GROUP BY b.guild_id, b.base_id
                """;
        jdbcTemplate.query(basesSql, rs -> {
            UUID guildId = rs.getObject("guild_id", UUID.class);
            basesByGuildId.computeIfAbsent(guildId, id -> new ArrayList<>()).add(new BaseSummaryView(
                    rs.getObject("base_id", UUID.class),
                    rs.getInt("pal_count")));
        });

        List<GuildSummaryView> result = new ArrayList<>();
        for (Map.Entry<UUID, String> guild : guildNameById.entrySet()) {
            result.add(new GuildSummaryView(
                    guild.getKey(),
                    guild.getValue(),
                    playersByGuildId.getOrDefault(guild.getKey(), List.of()),
                    basesByGuildId.getOrDefault(guild.getKey(), List.of())));
        }
        return result;
    }
}
