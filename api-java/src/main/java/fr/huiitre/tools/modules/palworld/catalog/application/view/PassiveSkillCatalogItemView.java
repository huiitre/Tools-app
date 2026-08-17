package fr.huiitre.tools.modules.palworld.catalog.application.view;

public record PassiveSkillCatalogItemView(
        String id,
        String name,
        String description,
        int rank,
        String rankIconUrl,
        boolean negative,
        boolean worldTree) {}
