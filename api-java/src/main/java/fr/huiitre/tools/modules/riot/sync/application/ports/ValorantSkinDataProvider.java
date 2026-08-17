package fr.huiitre.tools.modules.riot.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinSyncData;

public interface ValorantSkinDataProvider {
    List<ValorantSkinSyncData> fetchAll();
}
