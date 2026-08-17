package fr.huiitre.tools.modules.palworld.tierlist.infrastructure;

import java.util.List;

import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.tierlist.application.PalTierRankingRecord;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.PalTierRankingRepository;

public class PostgresPalTierRankingRepository implements PalTierRankingRepository {

    private final JdbcTemplate jdbcTemplate;

    public PostgresPalTierRankingRepository(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Override
    public void replaceAll(List<PalTierRankingRecord> rows) {
        jdbcTemplate.update("DELETE FROM tools_palworld.pal_tier_ranking");
        if (rows.isEmpty()) return;

        final String sql = """
                INSERT INTO tools_palworld.pal_tier_ranking (pal_id, category, source, tier)
                VALUES (?, ?, ?, ?)
                """;
        jdbcTemplate.batchUpdate(sql, rows, rows.size(), (ps, row) -> {
            ps.setLong(1, row.palId());
            ps.setString(2, row.category());
            ps.setString(3, row.source());
            ps.setString(4, row.tier());
        });
    }

    @Override
    public List<PalTierRankingRecord> findAll() {
        final String sql = """
                SELECT pal_id, category, source, tier
                FROM tools_palworld.pal_tier_ranking
                ORDER BY source, category
                """;
        return jdbcTemplate.query(sql, (rs, rowNum) -> new PalTierRankingRecord(
                rs.getLong("pal_id"), rs.getString("category"), rs.getString("source"), rs.getString("tier")));
    }
}
