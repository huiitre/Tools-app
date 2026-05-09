package fr.huiitre.tools.modules.dofus.workshop.application.dto;

import java.util.List;

public class WorkshopDetailResponse {
    
    private List<WorkshopItemDetailDto> items;
    private List<WorkshopLinkDto> links;
    private boolean isOwner;

    public WorkshopDetailResponse(List<WorkshopItemDetailDto> items, List<WorkshopLinkDto> links, boolean isOwner) {
        this.items = items;
        this.links = links;
        this.isOwner = isOwner;
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
