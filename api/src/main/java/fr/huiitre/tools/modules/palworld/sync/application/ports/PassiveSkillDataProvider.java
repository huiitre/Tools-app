package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.PassiveSkillSyncData;

public interface PassiveSkillDataProvider {
    List<PassiveSkillSyncData> fetchDisplayable();
}
