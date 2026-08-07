package fr.huiitre.tools.modules.palworld.sync.application.ports;

public interface ItemSyncRepository {
    Long upsertItem(String slug, String name, String iconUrl, Integer price, Integer maxStackCount, String category);
}
