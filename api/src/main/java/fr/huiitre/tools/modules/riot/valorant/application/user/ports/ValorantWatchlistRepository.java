package fr.huiitre.tools.modules.riot.valorant.application.user.ports;

import fr.huiitre.tools.modules.riot.valorant.application.user.view.ValorantWatchlistEntryView;

import java.util.List;

public interface ValorantWatchlistRepository {

    List<ValorantWatchlistEntryView> findAllByUserId(Long userId);

    Long add(Long userId, Long skinId);

    void remove(Long userId, Long skinId);

    boolean existsByUserIdAndSkinId(Long userId, Long skinId);
}
