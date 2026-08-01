package fr.huiitre.tools.modules.palworld.catalog.application.view;

public record WorkSuitabilitySummaryView(Long id, String slug, String name, String iconUrl, int level, Integer maxLevel,
        Integer starSegments, Integer emptySegments, boolean isPriority) {}
