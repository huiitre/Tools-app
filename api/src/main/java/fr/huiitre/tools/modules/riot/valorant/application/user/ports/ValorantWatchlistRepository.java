package fr.huiitre.tools.modules.riot.valorant.application.user.ports;

public interface ValorantWatchlistRepository {

    Long add(Long accountId, Long skinId);

    void remove(Long accountId, Long skinId);

    boolean existsByAccountIdAndSkinId(Long accountId, Long skinId);
}
