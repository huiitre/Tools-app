package fr.huiitre.tools.modules.riot.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.riot.sync.application.ValorantContentTierSyncData;

public interface ValorantContentTierDataProvider {
    List<ValorantContentTierSyncData> fetchAll();
}