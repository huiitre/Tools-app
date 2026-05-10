package fr.huiitre.tools.modules.riot.valorant.application.user.ports;

import java.time.LocalDate;
import java.util.List;
import java.util.Map;

public interface ValorantStoreHistoryRepository {
    Map<LocalDate, List<Long>> findAllRawByUserId(Long userId);
    Long add(Long userId, Long skinId, LocalDate seenAt);
    boolean existsByUserIdAndSkinIdAndDate(Long userId, Long skinId, LocalDate seenAt);
}

