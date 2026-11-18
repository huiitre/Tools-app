package fr.huiitre.tools.modules.dofus.breed.infrastructure;

import java.nio.file.Path;
import java.util.List;
import java.util.Map;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import fr.huiitre.tools.modules.core.filesystem.infrastructure.FileSystemChecker;
import fr.huiitre.tools.modules.dofus.breed.application.ports.BreedRepository;
import fr.huiitre.tools.modules.dofus.breed.domain.Breed;
import fr.huiitre.tools.modules.dofus.breed.domain.BreedSex;

public class PostgresBreedRepository implements BreedRepository {

    @Value("${tools.assets.base-path}")
    private Path assetsBasePath;

    private final JdbcTemplate jdbcTemplate;

    public PostgresBreedRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<Breed> BREED_ROW_MAPPER = (rs, rowNum) -> Breed.rehydrate(
            rs.getLong("id"),
            rs.getLong("asset_id"),
            rs.getLong("game_version_id"),
            rs.getString("name")
    );

    @Override
    public List<Breed> findAllByGameVersionId(Long gameVersionId) {
        final String sql = """
            SELECT id, asset_id, game_version_id, name
            FROM tools_dofus.breed
            WHERE game_version_id = ?
        """;

        return jdbcTemplate.query(sql, BREED_ROW_MAPPER, gameVersionId);
    }

    @Override
    public Long insert(Breed breed) {
        final String sql = """
            INSERT INTO tools_dofus.breed (asset_id, game_version_id, name)
            VALUES (?, ?, ?)
            RETURNING id
        """;

        return jdbcTemplate.queryForObject(
            sql,
            Long.class,
            breed.getAssetId(),
            breed.getGameVersionId(),
            breed.getName()
        );
    }

    @Override
    public void update(Breed breed) {
        final String sql = """
            UPDATE tools_dofus.breed
            SET name = ?
            WHERE id = ?
        """;

        jdbcTemplate.update(sql, breed.getName(), breed.getId());
    }

    //* Upsert par sexe : une image dont le fichier manque n'est pas enregistrée.
    @Override
    public void refreshImages(Long breedId, Map<BreedSex, Long> iconIdsBySex) {
        final String sql = """
            INSERT INTO tools_dofus.breed_image (breed_id, sex, icon_id)
            VALUES (?, ?, ?)
            ON CONFLICT (breed_id, sex) DO UPDATE SET icon_id = EXCLUDED.icon_id
        """;

        iconIdsBySex.forEach((sex, iconId) -> {
            if (headImageExists(iconId)) {
                jdbcTemplate.update(sql, breedId, sex.name(), iconId);
            }
        });
    }

    private boolean headImageExists(Long iconId) {
        return FileSystemChecker.exists(assetsBasePath.resolve(
                "tools_dofus/dofus3/img/class_head/2x/Head_" + iconId + "-64.png"));
    }
}
