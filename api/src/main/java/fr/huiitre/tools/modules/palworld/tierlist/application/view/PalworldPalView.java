package fr.huiitre.tools.modules.palworld.tierlist.application.view;

import java.util.List;

public record PalworldPalView(
    String name,
    String image,
    String href,
    List<PalworldWorkSkillView> workSkills,
    PalworldSpeedView speed
) {}
