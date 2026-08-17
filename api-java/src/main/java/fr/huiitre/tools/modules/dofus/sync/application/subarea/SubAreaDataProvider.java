package fr.huiitre.tools.modules.dofus.sync.application.subarea;

import java.util.List;

public interface SubAreaDataProvider {

    List<SubareaSyncData> fetchAll();
}
