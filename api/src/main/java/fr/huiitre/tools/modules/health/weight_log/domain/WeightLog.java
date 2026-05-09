package fr.huiitre.tools.modules.health.weight_log.domain;

import java.time.LocalDateTime;

public class WeightLog {
    private LocalDateTime logDate;
    private Double weight;
    private String notes;

    public WeightLog(Double weight, LocalDateTime logDate, String notes) {

        this.weight = weight;
        this.notes = notes;
        this.logDate = (logDate != null) ? logDate : LocalDateTime.now();

        validateFields();
    }

    public void update(
            Double weight,
            String notes,
            LocalDateTime logDate) {
        if (weight != null) {
            this.weight = weight;
        }

        if (notes != null) {
            this.notes = notes;
        }
        if (logDate != null) {
            this.logDate = logDate;
        }

        validateFields();
    }

    private void validateFields() {
        if (weight == null) {
            throw new IllegalArgumentException("WEIGHT_REQUIRED");
        }
        if (weight <= 0) {
            throw new IllegalArgumentException("WEIGHT_MUST_BE_POSITIVE");
        }
        if (logDate == null) {
            throw new IllegalArgumentException("LOG_DATE_REQUIRED");
        }
    }

    public LocalDateTime getLogDate() {
        return this.logDate;
    }

    public Double getWeight() {
        return this.weight;
    }

    public String getNotes() {
        return this.notes;
    }

}
