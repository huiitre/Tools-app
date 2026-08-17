package fr.huiitre.tools.modules.dofus.itemtype.domain;

public class ItemType {

    private final Long id;
    private final Long assetId;
    private final Long gameVersionId;
    private Long categoryId;
    private String name;

    private ItemType(
            Long id,
            Long assetId,
            Long gameVersionId,
            Long categoryId,
            String name) {
        this.id = id;
        this.assetId = assetId;
        this.gameVersionId = gameVersionId;
        this.categoryId = categoryId;
        this.name = name;

        validateFields();
    }

    public static ItemType rehydrate(
            Long id,
            Long assetId,
            Long gameVersionId,
            Long categoryId,
            String name) {
        if (id == null) {
            throw new IllegalArgumentException("ITEM_TYPE_ID_REQUIRED");
        }

        return new ItemType(
                id,
                assetId,
                gameVersionId,
                categoryId,
                name);
    }

    public static ItemType create(
            Long assetId,
            Long gameVersionId,
            Long categoryId,
            String name) {
        return new ItemType(
                null,
                assetId,
                gameVersionId,
                categoryId,
                name);
    }

    public void update(
            String name,
            Long categoryId) {

        if (name != null && !name.isBlank()) {
            this.name = name;
        }

        if (categoryId != null) {
            this.categoryId = categoryId;
        }

        validateFields();
    }

    private void validateFields() {
        if (assetId == null) {
            throw new IllegalArgumentException("ITEM_TYPE_ASSET_ID_REQUIRED");
        }
        if (gameVersionId == null) {
            throw new IllegalArgumentException("ITEM_TYPE_GAME_VERSION_ID_REQUIRED");
        }
        if (categoryId == null) {
            throw new IllegalArgumentException("ITEM_TYPE_CATEGORY_ID_REQUIRED");
        }
        if (name == null || name.isBlank()) {
            System.out.println("WARN: item type assetId=" + assetId + " has no name, skipped");
            throw new IllegalArgumentException("ITEM_TYPE_NAME_REQUIRED");
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

    public Long getCategoryId() {
        return categoryId;
    }

    public String getName() {
        return name;
    }
}
