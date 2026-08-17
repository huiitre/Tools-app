package fr.huiitre.tools.modules.dofus.monster.infrastructure;

import java.nio.file.Path;
import java.util.Collection;
import java.util.List;
import java.util.Set;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

import fr.huiitre.tools.modules.core.filesystem.infrastructure.FileSystemChecker;
import fr.huiitre.tools.modules.dofus.monster.application.dto.MonsterImageDto;
import fr.huiitre.tools.modules.dofus.monster.application.ports.MonsterRepository;
import fr.huiitre.tools.modules.dofus.monster.domain.Monster;

public class PostgresMonsterRepository implements MonsterRepository {
    
    @Value("${tools.assets.base-path}")
    private Path assetsBasePath;

    private final JdbcTemplate jdbcTemplate;
    private final NamedParameterJdbcTemplate namedParameterJdbcTemplate;

    public PostgresMonsterRepository(
            JdbcTemplate jdbcTemplate,
            NamedParameterJdbcTemplate namedParameterJdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
        this.namedParameterJdbcTemplate = namedParameterJdbcTemplate;
    }

    private static final RowMapper<Monster> MONSTER_ROW_MAPPER = (rs, rowNum) -> Monster.rehydrate(
            rs.getLong("id"),
            rs.getLong("asset_id"),
            rs.getLong("game_version_id"),
            rs.getString("name")
    );

    @Override
    public List<Monster> findAllByGameVersionId(Long gameVersionId) {
        
        final String sql = """
            SELECT id, asset_id, game_version_id, name
            FROM tools_dofus.monster
            WHERE game_version_id = ?
        """;

        return jdbcTemplate.query(sql, MONSTER_ROW_MAPPER, gameVersionId);
    }

    @Override
    public Long insert(Monster monster) {
        final String sql = """
            INSERT INTO tools_dofus.monster (asset_id, game_version_id, name)
            VALUES (?, ?, ?)
            RETURNING id
        """;
        return jdbcTemplate.queryForObject(
            sql,
            Long.class,
            monster.getAssetId(),
            monster.getGameVersionId(),
            monster.getName()
        );
    }

    @Override
    public void update(Monster monster) {
        final String sql = """
            UPDATE tools_dofus.monster
            SET asset_id = ?, game_version_id = ?, name = ?
            WHERE id = ?
        """;
        jdbcTemplate.update(sql, monster.getAssetId(), monster.getGameVersionId(), monster.getName(), monster.getId());
    }

    @Override
    public boolean refreshImages(Long monsterId, Long iconId) {

        ImageExistence images = checkItemImagesExistence(iconId);

        final String sqlDelete = """
            DELETE FROM tools_dofus.monster_image
            WHERE monster_id = ?
        """;
        
        final String sqlInsert = """
            INSERT INTO tools_dofus.monster_image (monster_id, icon_id, resolution)
            VALUES (?, ?, ?)
        """;

        jdbcTemplate.update(sqlDelete, monsterId);

        if (images.has1x()) {
            jdbcTemplate.update(sqlInsert, monsterId, iconId, "X1");
        }

        if (images.has2x()) {
            jdbcTemplate.update(sqlInsert, monsterId, iconId, "X2");
        }

        return true;
    }

    @Override
    public boolean refreshSubareas(Long monsterId, Collection<Long> subareaIds) {
        
        final String sqlDelete = """
            DELETE FROM tools_dofus.monster_subarea
            WHERE monster_id = ?
        """;

        final String sqlInsert = """
            INSERT INTO tools_dofus.monster_subarea (monster_id, subarea_id)
            VALUES (?, ?)
        """;

        jdbcTemplate.update(sqlDelete, monsterId);

        for (Long subareaId : subareaIds) {
            jdbcTemplate.update(sqlInsert, monsterId, subareaId);
        }

        return true;
    }

    @Override
    public boolean refreshDrops(Long monsterId, Collection<Long> itemIds) {
        
        final String sqlDelete = """
            DELETE FROM tools_dofus.monster_drop
            WHERE monster_id = ?
        """;

        final String sqlInsert = """
            INSERT INTO tools_dofus.monster_drop (monster_id, item_id)
            VALUES (?, ?)
        """;

        jdbcTemplate.update(sqlDelete, monsterId);

        for (Long itemId : itemIds) {
            jdbcTemplate.update(sqlInsert, monsterId, itemId);
        }

        return true;
    }

    private ImageExistence checkItemImagesExistence(Long iconId) {

        Path image1x = assetsBasePath.resolve(
                "tools_dofus/dofus3/img/monster/1x/" + iconId + "-64.png");

        Path image2x = assetsBasePath.resolve(
                "tools_dofus/dofus3/img/monster/2x/" + iconId + "-128.png");

        return new ImageExistence(
            FileSystemChecker.exists(image1x),
            FileSystemChecker.exists(image2x));
    }

    private static final RowMapper<MonsterImageDto> MONSTER_IMAGE_DTO_ROW_MAPPER = (rs, rowNum) -> new MonsterImageDto(
        rs.getLong("id"),
        rs.getLong("monster_id"),
        rs.getString("resolution"),
        rs.getLong("icon_id")
    );

    @Override
    public List<MonsterImageDto> findImageByMonsterId(Long monsterId) {
        final String sql = """
            SELECT id, monster_id, icon_id, resolution
            FROM tools_dofus.monster_image
            WHERE monster_id = ?
        """;

        return jdbcTemplate.query(sql, MONSTER_IMAGE_DTO_ROW_MAPPER, monsterId);
    }

    @Override
    public List<MonsterImageDto> findImageByMonsterIds(Collection<Long> monsterIds) {
        if (monsterIds.isEmpty()) {
            return List.of();
        }

        final String sql = """
            SELECT id, monster_id, icon_id, resolution
            FROM tools_dofus.monster_image
            WHERE monster_id = ANY(:monsterIds) 
        """;

        MapSqlParameterSource parameters = new MapSqlParameterSource();
        parameters.addValue("monsterIds", monsterIds.toArray(Long[]::new));

        return namedParameterJdbcTemplate.query(
            sql,
            parameters,
            MONSTER_IMAGE_DTO_ROW_MAPPER);
    }

    private record ImageExistence(boolean has1x, boolean has2x) {}
}
