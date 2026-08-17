package fr.huiitre.tools.modules.riot.valorant.application.catalog.ports;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantBundleView;

public interface ValorantBundleRepository {
    List<ValorantBundleView> findAll();
    Optional<ValorantBundleView> findById(Long id);
    Optional<ValorantBundleView> findByAssetId(UUID assetId);
}
