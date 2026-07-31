package fr.huiitre.tools.modules.riot.valorant.application.catalog.ports;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

public interface ValorantSkinRepository {

    List<ValorantSkinView> findAll(Long accountId);

    Optional<ValorantSkinView> findById(Long id, Long accountId);

    Optional<ValorantSkinView> findByLevelAssetId(UUID levelAssetId, Long accountId);

    Optional<ValorantSkinView> findByAssetId(UUID assetId, Long accountId);

    List<ValorantSkinView> findAllByWeaponId(Long weaponId, Long accountId);

    List<ValorantSkinView> findAllByTierUuid(UUID tierUuid, Long accountId);

    List<ValorantSkinView> findAllOwnedByAccountId(Long accountId);

    List<ValorantSkinView> findAllWatchedByAccountId(Long accountId);
}
