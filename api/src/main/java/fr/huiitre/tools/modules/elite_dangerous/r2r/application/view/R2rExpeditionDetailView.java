package fr.huiitre.tools.modules.elite_dangerous.r2r.application.view;

import java.time.LocalDateTime;
import java.util.List;
import java.util.UUID;

public class R2rExpeditionDetailView {

    private final UUID id;
    private final String name;
    private final String source;
    private final String routeData;
    private final int currentSystemIndex;
    private final List<Long> currentBodiesDone;
    private final LocalDateTime createdAt;
    private final LocalDateTime updatedAt;

    public R2rExpeditionDetailView(
            UUID id,
            String name,
            String source,
            String routeData,
            int currentSystemIndex,
            List<Long> currentBodiesDone,
            LocalDateTime createdAt,
            LocalDateTime updatedAt) {
        this.id = id;
        this.name = name;
        this.source = source;
        this.routeData = routeData;
        this.currentSystemIndex = currentSystemIndex;
        this.currentBodiesDone = currentBodiesDone;
        this.createdAt = createdAt;
        this.updatedAt = updatedAt;
    }

    public UUID getId() { return id; }
    public String getName() { return name; }
    public String getSource() { return source; }
    public String getRouteData() { return routeData; }
    public int getCurrentSystemIndex() { return currentSystemIndex; }
    public List<Long> getCurrentBodiesDone() { return currentBodiesDone; }
    public LocalDateTime getCreatedAt() { return createdAt; }
    public LocalDateTime getUpdatedAt() { return updatedAt; }
}
