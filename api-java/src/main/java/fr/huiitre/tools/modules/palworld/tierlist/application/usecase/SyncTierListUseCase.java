package fr.huiitre.tools.modules.palworld.tierlist.application.usecase;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.tierlist.application.PalTierRankingRecord;
import fr.huiitre.tools.modules.palworld.tierlist.application.TierEntrySyncData;
import fr.huiitre.tools.modules.palworld.tierlist.application.TierListSourceSyncData;
import fr.huiitre.tools.modules.palworld.tierlist.application.TierListSyncReport;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.PalLookupRepository;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.PalTierRankingRepository;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.TierListDataProvider;

@Service
@Transactional
public class SyncTierListUseCase implements SecuredUseCase {

    private final TierListDataProvider dataProvider;
    private final PalLookupRepository palLookupRepository;
    private final PalTierRankingRepository rankingRepository;

    public SyncTierListUseCase(TierListDataProvider dataProvider, PalLookupRepository palLookupRepository,
            PalTierRankingRepository rankingRepository) {
        this.dataProvider = dataProvider;
        this.palLookupRepository = palLookupRepository;
        this.rankingRepository = rankingRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public TierListSyncReport execute() {
        List<TierListSourceSyncData> sources = dataProvider.fetchAll();
        Map<String, Long> palIdByTribeUpper = palLookupRepository.findIdByTribeUpper();

        List<PalTierRankingRecord> rows = new ArrayList<>();
        int unmatched = 0;

        for (TierListSourceSyncData source : sources) {
            for (Map.Entry<String, List<TierEntrySyncData>> categoryEntry : source.categories().entrySet()) {
                for (TierEntrySyncData entry : categoryEntry.getValue()) {
                    Long palId = entry.tribe() == null ? null : palIdByTribeUpper.get(entry.tribe().toUpperCase());
                    if (palId == null) {
                        unmatched++;
                        continue;
                    }
                    rows.add(new PalTierRankingRecord(palId, categoryEntry.getKey(), source.source(), entry.tier()));
                }
            }
        }

        rankingRepository.replaceAll(rows);
        return new TierListSyncReport(rows.size(), unmatched);
    }
}
