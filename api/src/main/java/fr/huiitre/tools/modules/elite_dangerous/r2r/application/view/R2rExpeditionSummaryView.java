package fr.huiitre.tools.modules.elite_dangerous.r2r.application.view;

import java.time.LocalDateTime;
import java.util.UUID;

public class R2rExpeditionSummaryView {

    private final UUID id;
    private final String name;
    private final String source;
    private final int currentSystemIndex;
    private final int totalSystems;
    private final LocalDateTime createdAt;
    private final LocalDateTime updatedAt;

    public R2rExpeditionSummaryView(
            UUID id,
            String name,
            String source,
            int currentSystemIndex,
            int totalSystems,
            LocalDateTime createdAt,
            LocalDateTime updatedAt) {
        this.id = id;
        this.name = name;
        this.source = source;
        this.currentSystemIndex = currentSystemIndex;
        this.totalSystems = totalSystems;
        this.createdAt = createdAt;
        this.updatedAt = updatedAt;
    }

    public UUID getId() { return id; }
    public String getName() { return name; }
    public String getSource() { return source; }
    public int getCurrentSystemIndex() { return currentSystemIndex; }
    public int getTotalSystems() { return totalSystems; }
    public LocalDateTime getCreatedAt() { return createdAt; }
    public LocalDateTime getUpdatedAt() { return updatedAt; }
}
