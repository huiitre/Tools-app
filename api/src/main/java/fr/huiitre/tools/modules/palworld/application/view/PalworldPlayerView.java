package fr.huiitre.tools.modules.palworld.application.view;

public record PalworldPlayerView(
    String name,
    String accountName,
    String playerId,
    String userId,
    String ip,
    double ping,
    double locationX,
    double locationY,
    int level,
    int buildingCount
) {}
