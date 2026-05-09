package fr.huiitre.tools.modules.dofus.almanax.domain;

import java.util.List;

public class Almanax {

    private final Long id;
    private final Long assetId;
    private String name;
    private String description;
    private List<String> dates;
    private Long itemId;
    private Long itemQuantity;

    private Almanax(
            Long id,
            Long assetId,
            String name,
            String description,
            List<String> dates,
            Long itemId,
            Long itemQuantity) {
        this.id = id;
        this.assetId = assetId;
        this.name = name;
        this.description = parseDescription(description);
        this.dates = dates;
        this.itemId = itemId;
        this.itemQuantity = itemQuantity;
    }

    public static Almanax rehydrate(
            Long id,
            Long assetId,
            String name,
            String description,
            List<String> dates,
            Long itemId,
            Long itemQuantity) {
        if (id == null) {
            throw new IllegalArgumentException("ALMANAX_ID_REQUIRED");
        }

        return new Almanax(
                id,
                assetId,
                name,
                description,
                dates,
                itemId,
                itemQuantity);
    }

    public static Almanax create(
            Long assetId,
            String name,
            String description,
            List<String> dates,
            Long itemId,
            Long itemQuantity) {
        return new Almanax(
                null,
                assetId,
                name,
                description,
                dates,
                itemId,
                itemQuantity);
    }

    public void update(
            String name,
            String description,
            List<String> dates,
            Long itemId,
            Long itemQuantity) {
        if (name != null && !name.isBlank()) {
            this.name = name;
        }

        if (description != null && !description.isBlank()) {
            this.description = parseDescription(description);
        }

        if (dates != null && !dates.isEmpty()) {
            this.dates = dates;
        }

        if (itemId != null) {
            this.itemId = itemId;
        }

        if (itemQuantity != null) {
            this.itemQuantity = itemQuantity;
        }

        validateFields();
    }

    private void validateFields() {
        if (assetId == null) {
            throw new IllegalArgumentException("ALMANAX_ASSET_ID_REQUIRED");
        }
        if (name == null || name.isBlank()) {
            throw new IllegalArgumentException("ALMANAX_NAME_REQUIRED");
        }
        if (description == null || description.isBlank()) {
            throw new IllegalArgumentException("ALMANAX_DESCRIPTION_REQUIRED");
        }
        if (itemId == null) {
            throw new IllegalArgumentException("ALMANAX_ITEM_ID_REQUIRED");
        }
        if (itemQuantity == null) {
            throw new IllegalArgumentException("ALMANAX_ITEM_QUANTITY_REQUIRED");
        }
    }

    private String parseDescription(String raw) {
        return raw.replaceAll("\\{\\{monsterRace,\\d+::([^}]+)\\}\\}", "<b>$1</b>");
    }

    public Long getId() {
        return id;
    }

    public Long getAssetId() {
        return assetId;
    }

    public String getName() {
        return name;
    }

    public String getDescription() {
        return description;
    }

    public List<String> getDates() {
        return dates;
    }

    public Long getItemId() {
        return itemId;
    }

    public Long getItemQuantity() {
        return itemQuantity;
    }
}
