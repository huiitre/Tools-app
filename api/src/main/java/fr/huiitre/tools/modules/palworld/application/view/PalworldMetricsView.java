package fr.huiitre.tools.modules.palworld.application.view;

public record PalworldMetricsView(
    int currentPlayerNum,
    int serverFps,
    double serverFpsAverage,
    double serverFrameTime,
    int days,
    int maxPlayerNum,
    int baseCampNum,
    long uptime
) {}
