package fr.huiitre.tools.modules.dofus.catalogue.api.dto;

public class CatalogueSearchQuery {

    private Integer page = 1;
    private Integer pageSize = 20;
    private String q;
    private String sort;
    private Direction dir = Direction.ASC;

    public enum Direction {
        ASC,
        DESC
    }

    // GETTERS
    public Integer getPage() { return page; }
    public Integer getPageSize() { return pageSize; }
    public String getQ() { return q; }
    public String getSort() { return sort; }
    public Direction getDir() { return dir; }

    public void setPage(Integer page) {
        if (page == null || page < 1) {
            this.page = 1;
        } else {
            this.page = page;
        }
    }

    public void setPageSize(Integer pageSize) {
        if (pageSize == null || pageSize < 1) {
            this.pageSize = 20;
        } else if (pageSize > 100) {
            this.pageSize = 100;
        } else {
            this.pageSize = pageSize;
        }
    }

    public void setQ(String q) {
        this.q = q;
    }

    public void setSort(String sort) {
        this.sort = sort;
    }

    public void setDir(Direction dir) {
        this.dir = dir;
    }
}
