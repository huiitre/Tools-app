package fr.huiitre.tools.modules.dofus.sync.application.almanax;

import java.util.List;

public interface AlmanaxDataProvider {

    List<AlmanaxSyncData> fetchAll();
}
