package fr.huiitre.tools.modules.dofus.item.domain;

public class Item {
    private final Long id;
    private final Long assetId;
    private final Long gameVersionId;
    private Long itemTypeId;
    private String name;
    private Long level;
    private String description;

    private Item(
            Long id,
            Long assetId,
            Long gameVersionId,
            Long itemTypeId,
            String name,
            Long level,
            String description) {
        this.id = id;
        this.assetId = assetId;
        this.gameVersionId = gameVersionId;
        this.itemTypeId = itemTypeId;
        this.name = name;
        this.level = level;
        this.description = description;

        validateFields();
    }

    public static Item rehydrate(
            Long id,
            Long assetId,
            Long gameVersionId,
            Long itemTypeId,
            String name,
            Long level,
            String description) {
        if (id == null) {
            throw new IllegalArgumentException("ITEM_ID_REQUIRED");
        }

        return new Item(
                id,
                assetId,
                gameVersionId,
                itemTypeId,
                name,
                level,
                description);
    }

    public static Item create(
            Long assetId,
            Long gameVersionId,
            Long itemTypeId,
            String name,
            Long level,
            String description) {
        return new Item(
                null,
                assetId,
                gameVersionId,
                itemTypeId,
                name,
                level,
                description);
    }

    public void update(
            Long itemTypeId,
            String name,
            Long level,
            String description) {

        if (itemTypeId != null) {
            this.itemTypeId = itemTypeId;
        }

        if (name != null && !name.isBlank()) {
            this.name = name;
        }

        if (level != null) {
            this.level = level;
        }

        if (description != null) {
            this.description = description;
        }

        validateFields();
    }

    private void validateFields() {
        if (assetId == null) {
            throw new IllegalArgumentException("ITEM_ASSET_ID_REQUIRED");
        }

        if (gameVersionId == null) {
            throw new IllegalArgumentException("ITEM_GAME_VERSION_ID_REQUIRED");
        }

        if (itemTypeId == null) {
            throw new IllegalArgumentException("ITEM_ITEM_TYPE_ID_REQUIRED");
        }

        if (name == null || name.isBlank()) {
            throw new IllegalArgumentException("ITEM_NAME_REQUIRED");
        }

        if (level == null) {
            throw new IllegalArgumentException("ITEM_LEVEL_INVALID");
        }
    }

    public Long getId() {
        return id;
    }

    public Long getAssetId() {
        return assetId;
    }

    public Long getGameVersionId() {
        return gameVersionId;
    }

    public Long getItemTypeId() {
        return itemTypeId;
    }

    public String getName() {
        return name;
    }

    public Long getLevel() {
        return level;
    }

    public String getDescription() {
        return description;
    }
}
