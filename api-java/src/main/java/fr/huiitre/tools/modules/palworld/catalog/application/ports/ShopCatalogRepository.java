package fr.huiitre.tools.modules.palworld.catalog.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.catalog.application.view.MerchantView;

public interface ShopCatalogRepository {
    List<MerchantView> findAllMerchants();
}
