package fr.huiitre.tools.modules.riot.valorant.application.ports;

import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantUserSkinView;

import java.util.List;

public interface ValorantUserSkinRepository {

    List<ValorantUserSkinView> findAllByUserId(Long userId);

    Long add(Long userId, Long skinId);

    void remove(Long userId, Long skinId);

    boolean existsByUserIdAndSkinId(Long userId, Long skinId);
}
