package fr.huiitre.tools.modules.riot.sync.application;

public interface ValorantSkinChromaSyncRepository {
    void deleteAll();
    void save(Long skinId, ValorantSkinChromaSyncData data);
}
