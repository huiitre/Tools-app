package fr.huiitre.tools.modules.riot.valorant.application.ports;

import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantTokenView;

public interface RiotAuthPort {

    ValorantTokenView refresh(String refreshToken);
}
