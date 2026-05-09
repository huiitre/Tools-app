package fr.huiitre.tools.modules.riot.sync.application;

public interface ValorantSkinLevelSyncRepository {
    void deleteAll();
    void save(Long skinId, ValorantSkinLevelSyncData data);
}
