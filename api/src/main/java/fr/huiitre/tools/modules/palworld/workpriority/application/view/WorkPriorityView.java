package fr.huiitre.tools.modules.palworld.workpriority.application.view;

public record WorkPriorityView(
        Long id,
        String code,
        String name,
        String iconUrl,
        int priority,
        WorkSuitabilityRefView workSuitability) {}
