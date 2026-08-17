package fr.huiitre.tools.modules.dofus.sync.application.monster;

import java.util.List;

public interface MonsterDataProvider {

    List<MonsterSyncData> fetchAll();
}
