package fr.huiitre.tools.modules.dofus.workshop.application.dto;

import java.util.List;

public class WorkshopDetailResponse {
    
    private String name;
    private List<WorkshopBreedDto> breeds;
    private List<WorkshopJobDto> jobs;
    private List<WorkshopItemDetailDto> items;
    private List<WorkshopLinkDto> links;
    private boolean isOwner;

    public WorkshopDetailResponse(
            String name,
            List<WorkshopBreedDto> breeds,
            List<WorkshopJobDto> jobs,
            List<WorkshopItemDetailDto> items,
            List<WorkshopLinkDto> links,
            boolean isOwner) {
        this.name = name;
        this.breeds = breeds;
        this.jobs = jobs;
        this.items = items;
        this.links = links;
        this.isOwner = isOwner;
    }

    public String getName() {
        return name;
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

    public List<WorkshopLinkDto> getLinks() {
        return links;
    }

    public boolean isOwner() {
        return isOwner;
    }
}
