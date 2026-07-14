package fr.huiitre.tools.modules.palworld.tierlist.application.view;

import java.util.List;

public record PalworldTierGroupView(
    String tier,
    List<PalworldPalView> pals
) {}
