package fr.huiitre.tools.modules.dofus.sync.application.area;

import java.util.List;

public interface AreaDataProvider {

    List<AreaSyncData> fetchAll();
}
