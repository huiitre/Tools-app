package fr.huiitre.tools.modules.palworld.serverdata.infrastructure;

import java.time.OffsetDateTime;
import java.sql.Array;
import java.sql.SQLException;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.serverdata.application.ports.ServerInventoryQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.ServerDataInventoryView;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.ServerPalInventoryView;

public class PostgresServerInventoryQueryRepository implements ServerInventoryQueryRepository {

    private final JdbcTemplate jdbcTemplate;
    private final PostgresGuildQueryRepository guildQueryRepository;

    public PostgresServerInventoryQueryRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
        this.guildQueryRepository = new PostgresGuildQueryRepository(jdbcTemplate);
    }

    @Override
    public ServerDataInventoryView getCurrentInventory() {
        OffsetDateTime lastSyncedAt = jdbcTemplate.queryForObject(
                "SELECT MAX(extracted_at) FROM tools_palworld.server_snapshot_import",
                (rs, rowNum) -> rs.getObject(1, OffsetDateTime.class));

        List<ServerPalInventoryView> pals = jdbcTemplate.query("""
                SELECT instance_id, pal_id, owner_player_uid, base_id, storage_location, container_id, gender,
                       favorite_index, passive_skill_ids, last_seen_at
                FROM tools_palworld.pal_instance
                WHERE is_present = TRUE AND pal_id IS NOT NULL
                ORDER BY pal_id, instance_id
                """, (rs, rowNum) -> new ServerPalInventoryView(
                rs.getObject("instance_id", UUID.class),
                (Long) rs.getObject("pal_id"),
                rs.getObject("owner_player_uid", UUID.class),
                rs.getObject("base_id", UUID.class),
                rs.getString("storage_location"),
                rs.getObject("container_id", UUID.class),
                rs.getString("gender"),
                (Integer) rs.getObject("favorite_index"),
                stringList(rs.getArray("passive_skill_ids")),
                rs.getObject("last_seen_at", OffsetDateTime.class)));

        return new ServerDataInventoryView(lastSyncedAt, guildQueryRepository.findAllWithMembersAndBases(), pals);
    }

    private List<String> stringList(Array array) throws SQLException {
        if (array == null) return List.of();
        Object[] values = (Object[]) array.getArray();
        List<String> result = new ArrayList<>(values.length);
        for (Object value : values) {
            if (value != null) result.add(value.toString());
        }
        return result;
    }
}
