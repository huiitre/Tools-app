package fr.huiitre.tools.modules.dofus.workshop.domain;

import java.time.LocalDateTime;

public class WorkshopLink {
    private final Long id;
    private final LinkSource source;
    private final String url;
    private String label;
    private final LocalDateTime createdAt;

    private WorkshopLink(Long id, LinkSource source, String url, String label, LocalDateTime createdAt) {
        validate(url, label);
        this.id = id;
        this.source = source;
        this.url = url;
        this.label = label;
        this.createdAt = createdAt;
    }

    public static WorkshopLink rehydrate(Long id, LinkSource source, String url, String label, LocalDateTime createdAt) {
        return new WorkshopLink(id, source, url, label, createdAt);
    }

    public static WorkshopLink create(LinkSource source, String url, String label) {
        return new WorkshopLink(null, source, url, label, LocalDateTime.now());
    }

    private void validate(String url, String label) {
        if (url == null || url.isBlank()) {
            throw new IllegalArgumentException("L'URL ne peut pas être vide.");
        }
        if (label == null || label.isBlank()) {
            throw new IllegalArgumentException("Le titre ne peut pas être vide.");
        }
    }

    public void updateLabel(String label) {
        if (label == null || label.isBlank()) {
            throw new IllegalArgumentException("Le titre ne peut pas être vide.");
        }
        this.label = label;
    }

    public Long getId() {
        return id;
    }

    public LinkSource getSource() {
        return source;
    }

    public String getUrl() {
        return url;
    }

    public String getLabel() {
        return label;
    }

    public LocalDateTime getCreatedAt() {
        return createdAt;
    }
}
