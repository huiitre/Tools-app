package fr.huiitre.tools.modules.palworld.catalog.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.catalog.application.view.ItemCatalogView;

public interface ItemCatalogRepository {
    List<ItemCatalogView> findAll();
}
