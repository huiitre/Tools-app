package fr.huiitre.tools.modules.palworld.tierlist.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.tierlist.application.PalTierRankingRecord;

public interface PalTierRankingRepository {
    void replaceAll(List<PalTierRankingRecord> rows);

    List<PalTierRankingRecord> findAll();
}
