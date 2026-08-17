package fr.huiitre.tools.modules.palworld.application.view;

public record PalworldBasePalView(
    String name,
    String characterClass,
    int level,
    int hp,
    int maxHp,
    String guildId,
    double locationX,
    double locationY,
    double locationZ,
    long mapX,
    long mapY
) {}
