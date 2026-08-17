package fr.huiitre.tools.modules.palworld.application.view;

import java.util.List;

public record PalworldGameDataView(
    String time,
    double fps,
    double averageFps,
    String inGameTime,
    int inGameDays,
    List<PalworldGamePlayerView> players,
    List<PalworldBaseView> bases,
    List<PalworldBasePalView> basePals
) {}
