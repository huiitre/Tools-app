package fr.huiitre.tools.modules.dofus.subarea.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.dofus.subarea.domain.Subarea;

public interface SubareaRepository {
    
    List<Subarea> findAllByGameVersionId(Long gameVersionId);

    void insert(Subarea subarea);

    void update(Subarea subarea);
}
