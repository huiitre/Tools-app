package fr.huiitre.tools.modules.dofus.workshop.domain;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public class Workshop {
    
    private final Long id;
    private String name;
    private boolean active;
    private boolean pinned;
    private List<WorkshopLink> links;

    private Workshop(Long id, String name, boolean active, boolean pinned, List<WorkshopLink> links) {
        validateName(name);
        this.id = id;
        this.name = name;
        this.active = active;
        this.pinned = pinned;
        this.links = links != null ? new ArrayList<>(links) : new ArrayList<>();
    }

    public static Workshop rehydrate(Long id, String name, boolean active, boolean pinned, List<WorkshopLink> links) {
        return new Workshop(id, name, active, pinned, links);
    }

    public static Workshop create(String name) {
        return new Workshop(null, name, true, false, new ArrayList<>());
    }

    public void addLink(WorkshopLink link) {
        if (link == null) {
            throw new IllegalArgumentException("Le lien ne peut pas être nul.");
        }
        this.links.add(link);
    }

    public List<WorkshopLink> getLinks() {
        return Collections.unmodifiableList(links);
    }

    public void update(String name, boolean active, boolean pinned) {
        validateName(name);
        this.name = name;
        this.active = active;
        this.pinned = pinned;
    }

    private void validateName(String name) {
        if (name == null || name.isBlank()) {
            throw new IllegalArgumentException("Le nom de l'atelier ne peut pas être vide.");
        }

        if (name.length() > 30) {
            throw new IllegalArgumentException("Le nom de l'atelier ne peut pas dépasser 30 caractères.");
        }
    }

    public Long getId() {
        return id;
    }

    public String getName() {
        return name;
    }

    public boolean isActive() {
        return active;
    }

    public boolean isPinned() {
        return pinned;
    }
}