package fr.huiitre.tools.modules.palworld.serverdata.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.serverdata.application.view.GuildSummaryView;

public interface GuildQueryRepository {
    List<GuildSummaryView> findAllWithMembersAndBases();
}
