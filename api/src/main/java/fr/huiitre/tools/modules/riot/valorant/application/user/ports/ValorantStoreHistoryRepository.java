package fr.huiitre.tools.modules.riot.valorant.application.user.ports;

import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantStoreHistoryView;
import java.util.List;

public interface ValorantStoreHistoryRepository {
    List<ValorantStoreHistoryView> findAllByUserId(Long userId);
    Long add(Long userId, Long skinId);
    boolean existsByUserIdAndSkinIdAndDate(Long userId, Long skinId);
}
