package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.MerchantSyncData;

public interface MerchantDataProvider {
    List<MerchantSyncData> fetchAll();
}
