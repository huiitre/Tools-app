package fr.huiitre.tools.modules.riot.valorant.application.catalog.ports;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantSkinView;

public interface ValorantSkinRepository {

    List<ValorantSkinView> findAll();

    Optional<ValorantSkinView> findById(Long id);

    Optional<ValorantSkinView> findByLevelAssetId(UUID levelAssetId);

    Optional<ValorantSkinView> findByAssetId(UUID assetId);

    List<ValorantSkinView> findAllByWeaponId(Long weaponId);

    List<ValorantSkinView> findAllByTierUuid(UUID tierUuid);
}
