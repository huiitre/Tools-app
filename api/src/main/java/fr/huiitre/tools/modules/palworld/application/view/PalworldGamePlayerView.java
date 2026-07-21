package fr.huiitre.tools.modules.palworld.application.view;

public record PalworldGamePlayerView(
    String name,
    String userId,
    String ip,
    int level,
    int hp,
    int maxHp,
    String guildId,
    String guildName,
    double locationX,
    double locationY,
    double locationZ,
    long mapX,
    long mapY,
    double rotationZ,
    PalworldActivePalView activePal
) {}
