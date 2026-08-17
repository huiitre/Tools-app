package fr.huiitre.tools.modules.dofus.area.domain;

public class Area {

    private final Long id;
    private final Long assetId;
    private final Long gameVersionId;
    private String name;

    private Area(
        Long id,
        Long assetId,
        Long gameVersionId,
        String name
    ) {
        this.id = id;
        this.assetId = assetId;
        this.gameVersionId = gameVersionId;
        this.name = name;
    }

    public static Area rehydrate(
        Long id,
        Long assetId,
        Long gameVersionId,
        String name
    ) {
        if (id == null) {
            throw new IllegalArgumentException("AREA_ID_REQUIRED");
        }

        return new Area(
            id,
            assetId,
            gameVersionId,
            name
        );
    }

    public static Area create(
        Long assetId,
        Long gameVersionId,
        String name
    ) {
        return new Area(
            null,
            assetId,
            gameVersionId,
            name
        );
    }

    public void update(
        String name
    ) {
        if (name != null && !name.isBlank()) {
            this.name = name;
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

    public String getName() {
        return name;
    }
}
