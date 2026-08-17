package fr.huiitre.tools.modules.elite_dangerous.r2r.application.ports;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import fr.huiitre.tools.modules.elite_dangerous.r2r.application.view.R2rExpeditionDetailView;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.view.R2rExpeditionSummaryView;
import fr.huiitre.tools.modules.elite_dangerous.r2r.domain.R2rExpedition;

public interface R2rExpeditionRepository {
    List<R2rExpeditionSummaryView> findAllByUserId(Long userId);
    Optional<R2rExpeditionDetailView> findByIdAndUserId(UUID id, Long userId);
    Optional<String> findRouteDataByIdAndUserId(UUID id, Long userId);
    UUID save(Long userId, R2rExpedition expedition);
    void updateProgress(UUID id, Long userId, int currentSystemIndex, List<Long> currentBodiesDone);
    void rename(UUID id, Long userId, String name);
    void delete(UUID id, Long userId);
    boolean existsByIdAndUserId(UUID id, Long userId);
    int countByUserId(Long userId);
}
