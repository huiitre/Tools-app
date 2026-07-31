package fr.huiitre.tools.modules.riot.valorant.application.user.ports;

import java.time.LocalDate;
import java.util.List;
import java.util.Map;

public interface ValorantStoreHistoryRepository {
    Map<LocalDate, List<Long>> findAllRawByAccountId(Long accountId);
    Long add(Long accountId, Long skinId, LocalDate seenAt);
    boolean existsByAccountIdAndSkinIdAndDate(Long accountId, Long skinId, LocalDate seenAt);
}
