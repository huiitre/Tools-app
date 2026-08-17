package fr.huiitre.tools.modules.palworld.tierlist.application.usecase;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.tierlist.application.PalTierRankingRecord;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.PalTierRankingRepository;
import fr.huiitre.tools.modules.palworld.tierlist.application.view.TierGroupView;

@Service
public class GetTierListUseCase implements SecuredUseCase {

    private static final List<String> TIER_ORDER = List.of("S", "A", "B", "C", "D");

    private final PalTierRankingRepository rankingRepository;

    public GetTierListUseCase(PalTierRankingRepository rankingRepository) {
        this.rankingRepository = rankingRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public Map<String, Map<String, List<TierGroupView>>> execute() {
        Map<String, Map<String, Map<String, List<Long>>>> bySourceCategoryTier = new LinkedHashMap<>();

        for (PalTierRankingRecord row : rankingRepository.findAll()) {
            bySourceCategoryTier
                    .computeIfAbsent(row.source(), s -> new LinkedHashMap<>())
                    .computeIfAbsent(row.category(), c -> new LinkedHashMap<>())
                    .computeIfAbsent(row.tier(), t -> new ArrayList<>())
                    .add(row.palId());
        }

        Map<String, Map<String, List<TierGroupView>>> result = new LinkedHashMap<>();
        for (Map.Entry<String, Map<String, Map<String, List<Long>>>> sourceEntry : bySourceCategoryTier.entrySet()) {
            Map<String, List<TierGroupView>> categories = new LinkedHashMap<>();
            for (Map.Entry<String, Map<String, List<Long>>> categoryEntry : sourceEntry.getValue().entrySet()) {
                List<TierGroupView> groups = categoryEntry.getValue().entrySet().stream()
                        .map(tierEntry -> new TierGroupView(tierEntry.getKey(), tierEntry.getValue()))
                        .sorted(Comparator.comparingInt(group -> tierRank(group.tier())))
                        .toList();
                categories.put(categoryEntry.getKey(), groups);
            }
            result.put(sourceEntry.getKey(), categories);
        }
        return result;
    }

    private int tierRank(String tier) {
        int index = TIER_ORDER.indexOf(tier);
        return index < 0 ? Integer.MAX_VALUE : index;
    }
}
