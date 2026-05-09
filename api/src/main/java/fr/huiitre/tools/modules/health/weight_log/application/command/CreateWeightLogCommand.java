package fr.huiitre.tools.modules.health.weight_log.application.command;

import java.time.LocalDateTime;

public class CreateWeightLogCommand {

    private final Double weight;
    private final String notes;
    private final LocalDateTime logDate;

    public CreateWeightLogCommand(
            Double weight,
            String notes,
            LocalDateTime logDate) {
        this.weight = weight;
        this.notes = notes;
        this.logDate = logDate;
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
