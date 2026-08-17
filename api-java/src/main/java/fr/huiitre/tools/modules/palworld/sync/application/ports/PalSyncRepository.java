package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.time.OffsetDateTime;
import java.util.List;
import java.util.Map;

import fr.huiitre.tools.modules.palworld.sync.application.PalSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.view.PalRefView;

public interface PalSyncRepository {

    List<PalRefView> findAll();
    Long save(PalSyncData data);
    void update(Long id, PalSyncData data);
    void delete(Long id);

    void upsertSource(Long palId, String slug, String sourceUrl, String rawPayloadJson, OffsetDateTime fetchedAt);

    Long findOrCreateItem(String slug, String name, String iconUrl);

    void deleteAllChildren();

    void saveElements(Long palId, PalSyncData data, Map<String, Long> elementIdByName);
    void saveWorkSuitabilities(Long palId, PalSyncData data, Map<String, Long> workSuitabilityIdBySlug);
    void saveActiveSkills(Long palId, PalSyncData data, Map<String, Long> skillIdByName);
    void savePassiveSkills(Long palId, PalSyncData data);
    void saveDrops(Long palId, PalSyncData data);
}
