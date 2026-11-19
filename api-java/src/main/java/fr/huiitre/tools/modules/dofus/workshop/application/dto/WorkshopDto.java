package fr.huiitre.tools.modules.dofus.workshop.application.dto;

import java.util.List;

public class WorkshopDto {
    
    private final Long id;
    private final String name;
    private final boolean active;
    private final List<WorkshopTagDto> tags;
    private final List<WorkshopLinkDto> links;
    private final boolean pinned;
    private final List<WorkshopBreedDto> breeds;
    private final List<WorkshopJobDto> jobs;
    private final List<WorkshopItemDetailDto> items;

    public WorkshopDto(
        Long id,
        String name,
        boolean active,
        List<WorkshopTagDto> tags,
        List<WorkshopLinkDto> links,
        boolean pinned,
        List<WorkshopBreedDto> breeds,
        List<WorkshopJobDto> jobs,
        List<WorkshopItemDetailDto> items
    ) {
        this.id = id;
        this.name = name;
        this.active = active;
        this.tags = tags;
        this.links = links;
        this.pinned = pinned;
        this.breeds = breeds;
        this.jobs = jobs;
        this.items = items;
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

    public List<WorkshopTagDto> getTags() {
        return tags;
    }

    public List<WorkshopLinkDto> getLinks() {
        return links;
    }

    public boolean isPinned() {
        return pinned;
    }

    public List<WorkshopBreedDto> getBreeds() {
        return breeds;
    }

    public List<WorkshopJobDto> getJobs() {
        return jobs;
    }

    public List<WorkshopItemDetailDto> getItems() {
        return items;
    }
}
