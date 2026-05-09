package fr.huiitre.tools.modules.riot.valorant.application.catalog.ports;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantWeaponView;

public interface ValorantWeaponRepository {
    List<ValorantWeaponView> findAll();
    Optional<ValorantWeaponView> findById(Long id);
}
