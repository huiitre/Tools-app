package fr.huiitre.tools.modules.dofus.workshop.application.dto;

import java.util.List;

public class WorkshopDetailDto {
    
    private final Long id;
    private final List<WorkshopItemDetailDto> items;

    public WorkshopDetailDto(
        Long id,
        List<WorkshopItemDetailDto> items
    ) {
        this.id = id;
        this.items = items;
    }

    public Long getId() {
        return id;
    }

    public List<WorkshopItemDetailDto> getItems() {
        return items;
    }
}
