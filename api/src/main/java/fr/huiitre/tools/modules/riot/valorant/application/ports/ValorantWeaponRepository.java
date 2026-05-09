package fr.huiitre.tools.modules.riot.valorant.application.ports;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantWeaponView;

public interface ValorantWeaponRepository {
    List<ValorantWeaponView> findAll();
    Optional<ValorantWeaponView> findById(Long id);
}
