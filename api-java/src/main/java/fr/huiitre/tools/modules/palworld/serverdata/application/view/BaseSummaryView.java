package fr.huiitre.tools.modules.palworld.serverdata.application.view;

import java.util.UUID;

public record BaseSummaryView(UUID baseId, int palCount, Double positionX, Double positionY, Double positionZ,
        Double rotationX, Double rotationY, Double rotationZ, Double rotationW, Double areaRange) {}
