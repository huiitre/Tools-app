package fr.huiitre.tools.modules.palworld.catalog.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.catalog.application.view.PassiveSkillCatalogItemView;

public interface PassiveSkillCatalogRepository {
    List<PassiveSkillCatalogItemView> findAll();
}
