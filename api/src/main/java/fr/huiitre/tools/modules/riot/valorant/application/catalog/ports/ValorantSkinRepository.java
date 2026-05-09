package fr.huiitre.tools.modules.riot.valorant.application.catalog.ports;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

public interface ValorantSkinRepository {

    List<ValorantSkinView> findAll(Long userId);

    Optional<ValorantSkinView> findById(Long id, Long userId);

    Optional<ValorantSkinView> findByLevelAssetId(UUID levelAssetId, Long userId);

    Optional<ValorantSkinView> findByAssetId(UUID assetId, Long userId);

    List<ValorantSkinView> findAllByWeaponId(Long weaponId, Long userId);

    List<ValorantSkinView> findAllByTierUuid(UUID tierUuid, Long userId);

    List<ValorantSkinView> findAllOwnedByUserId(Long userId);

    List<ValorantSkinView> findAllWatchedByUserId(Long userId);
}
