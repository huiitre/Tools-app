package fr.huiitre.tools.modules.riot.sync.application;

import java.util.List;

public interface ValorantBundleDataProvider {
    List<ValorantBundleSyncData> fetchAll();
}
