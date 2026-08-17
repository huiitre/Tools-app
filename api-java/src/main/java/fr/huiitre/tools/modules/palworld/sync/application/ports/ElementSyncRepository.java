package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.ElementSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.view.ElementRefView;

public interface ElementSyncRepository {
    List<ElementRefView> findAll();
    Long save(ElementSyncData data);
    void update(Long id, ElementSyncData data);
    void delete(Long id);
}
