package fr.huiitre.tools.modules.dofus.catalogue.application.dto;

public class CatalogueColumnDto {

    /* =========================
       IDENTITÉ
    ========================= */

    private final String key;
    private final String label;
    private final String description;

    /* =========================
       COMPORTEMENT
    ========================= */

    private final boolean visible;
    private final boolean userToggle;
    private final boolean sortable;

    /* =========================
       LAYOUT (TABLE)
       — compatible TanStack
    ========================= */

    private final int size;
    private final int minSize;
    private final int maxSize;

    /* =========================
       CONSTRUCTOR
    ========================= */

    public CatalogueColumnDto(
            String key,
            String label,
            String description,
            boolean visible,
            boolean userToggle,
            boolean sortable,
            int size,
            int minSize,
            int maxSize
    ) {
        this.key = key;
        this.label = label;
        this.description = description;
        this.visible = visible;
        this.userToggle = userToggle;
        this.sortable = sortable;
        this.size = size;
        this.minSize = minSize;
        this.maxSize = maxSize;
    }

    /* =========================
       GETTERS — MÉTIER
    ========================= */

    public String getKey() {
        return key;
    }

    public String getLabel() {
        return label;
    }

    public String getDescription() {
        return description;
    }

    public boolean isVisible() {
        return visible;
    }

    public boolean isUserToggle() {
        return userToggle;
    }

    public boolean isSortable() {
        return sortable;
    }

    /* =========================
       GETTERS — LAYOUT
    ========================= */

    public int getSize() {
        return size;
    }

    public int getMinSize() {
        return minSize;
    }

    public int getMaxSize() {
        return maxSize;
    }
}
