package fr.huiitre.tools.modules.palworld.serverdata.application.ports;

import java.util.List;
import java.util.UUID;

import fr.huiitre.tools.modules.palworld.serverdata.application.view.PalInstanceSnapshotView;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.PalInstanceSummaryView;

public interface PalInstanceQueryRepository {
    List<PalInstanceSummaryView> findByBaseId(UUID baseId);
    List<PalInstanceSnapshotView> findHistoryByInstanceId(UUID instanceId);
}
