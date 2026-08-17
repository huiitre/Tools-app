package fr.huiitre.tools.modules.riot.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.riot.sync.application.ValorantBundleSyncData;

public interface ValorantBundleDataProvider {
    List<ValorantBundleSyncData> fetchAll();
}
