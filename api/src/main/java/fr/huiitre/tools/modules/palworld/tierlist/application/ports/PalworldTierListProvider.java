package fr.huiitre.tools.modules.palworld.tierlist.application.ports;

import fr.huiitre.tools.modules.palworld.tierlist.application.view.PalworldTierGroupView;

import java.util.List;
import java.util.Map;

public interface PalworldTierListProvider {

    Map<String, List<PalworldTierGroupView>> getTierLists();
}
