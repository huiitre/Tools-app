package fr.huiitre.tools.modules.dofus.almanax.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.dofus.almanax.domain.Almanax;

public interface AlmanaxRepository {

    List<Almanax> findAll();

    Long save(Almanax almanax);

    void update(Almanax almanax);
}
