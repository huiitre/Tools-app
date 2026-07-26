package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.SkillSyncData;

public interface SkillDataProvider {
    List<SkillSyncData> fetchAll();
}
