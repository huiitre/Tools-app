package fr.huiitre.tools.modules.dofus.catalogue.application.dto;

import java.util.List;

import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;

public class CatalogueSearchResponse {

    private final List<CatalogueColumnDto> columns;
    private final List<ItemDto> items;

    private final int page;
    private final int pageSize;
    private final long total;

    private final Integer previousPage;
    private final Integer nextPage;
    private final Integer lastPage;

    public CatalogueSearchResponse(
            List<CatalogueColumnDto> columns,
            List<ItemDto> items,
            int page,
            int pageSize,
            long total,
            Integer previousPage,
            Integer nextPage,
            Integer lastPage) {
        this.columns = columns;
        this.items = items;
        this.page = page;
        this.pageSize = pageSize;
        this.total = total;
        this.previousPage = previousPage;
        this.nextPage = nextPage;
        this.lastPage = lastPage;
    }

    public List<CatalogueColumnDto> getColumns() {
        return columns;
    }

    public List<ItemDto> getItems() {
        return items;
    }

    public int getPage() {
        return page;
    }

    public int getPageSize() {
        return pageSize;
    }

    public long getTotal() {
        return total;
    }

    public Integer getPreviousPage() {
        return previousPage;
    }

    public Integer getNextPage() {
        return nextPage;
    }

    public Integer getLastPage() {
        return lastPage;
    }
}
