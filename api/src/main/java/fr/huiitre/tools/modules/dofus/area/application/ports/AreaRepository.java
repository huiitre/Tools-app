package fr.huiitre.tools.modules.dofus.area.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.dofus.area.domain.Area;

public interface AreaRepository {
    
    List<Area> findAllByGameVersionId(Long gameVersionId);

    void insert(Area area);

    void update(Area area);
}
