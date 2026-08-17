package fr.huiitre.tools.modules.palworld.application.view;

public record PalworldBaseView(
    String name,
    String guildId,
    String guildName,
    double locationX,
    double locationY,
    double locationZ,
    long mapX,
    long mapY
) {}
