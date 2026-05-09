package fr.huiitre.tools.modules.health.weight_log.application.view;

import java.time.LocalDateTime;

public class WeightLogView {
    private final Long id;
    private final Double weight;
    private final String notes;
    private final LocalDateTime logDate;

    public WeightLogView(
            Long id,
            Double weight,
            LocalDateTime logDate,
            String notes) {
        this.id = id;
        this.weight = weight;
        this.notes = notes;
        this.logDate = logDate;
    }

    public Long getId() {
        return this.id;
    }

    public Double getWeight() {
        return this.weight;
    }

    public String getNotes() {
        return this.notes;
    }

    public LocalDateTime getLogDate() {
        return this.logDate;
    }
}
