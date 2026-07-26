package fr.huiitre.tools.modules.palworld.serverdata.application.ports;

import java.util.Map;

public interface PalLookupRepository {
    Map<String, Long> findIdByTribeUpper();
}
