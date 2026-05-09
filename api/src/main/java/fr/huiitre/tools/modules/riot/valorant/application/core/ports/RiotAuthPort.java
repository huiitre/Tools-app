package fr.huiitre.tools.modules.riot.valorant.application.core.ports;

import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantTokenView;

public interface RiotAuthPort {

    ValorantTokenView refresh(String refreshToken);
}
