package fr.huiitre.tools.modules.riot.valorant.application.user.ports;

public interface ValorantUserSkinRepository {

    Long add(Long userId, Long skinId);

    void remove(Long userId, Long skinId);

    boolean existsByUserIdAndSkinId(Long userId, Long skinId);
}
