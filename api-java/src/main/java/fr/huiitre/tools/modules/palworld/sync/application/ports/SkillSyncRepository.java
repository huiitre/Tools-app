package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.time.OffsetDateTime;
import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.SkillSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.view.SkillRefView;

public interface SkillSyncRepository {
    List<SkillRefView> findAll();
    Long save(SkillSyncData data, Long elementId);
    void update(Long id, SkillSyncData data, Long elementId);
    void delete(Long id);
    void upsertSource(Long skillId, String slug, String sourceUrl, String rawPayloadJson, OffsetDateTime fetchedAt);
}
