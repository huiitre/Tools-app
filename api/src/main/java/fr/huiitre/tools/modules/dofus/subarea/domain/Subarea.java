package fr.huiitre.tools.modules.dofus.subarea.domain;

public class Subarea {
    
    private final Long id;
    private final Long assetId;
    private final Long gameVersionId;
    private Long areaId;
    private String name;

    private Subarea(
        Long id,
        Long assetId,
        Long gameVersionId,
        Long areaId,
        String name
    ) {
        this.id = id;
        this.assetId = assetId;
        this.gameVersionId = gameVersionId;
        this.areaId = areaId;
        this.name = name;
    }

    public static Subarea rehydrate(
        Long id,
        Long assetId,
        Long gameVersionId,
        Long areaId,
        String name
    ) {
        if (id == null) {
            throw new IllegalArgumentException("SUBAREA_ID_REQUIRED");
        }

        return new Subarea(
            id,
            assetId,
            gameVersionId,
            areaId,
            name
        );
    }

    public static Subarea create(
        Long assetId,
        Long gameVersionId,
        Long areaId,
        String name
    ) {
        return new Subarea(
            null,
            assetId,
            gameVersionId,
            areaId,
            name
        );
    }

    public void update(
        Long areaId,
        String name
    ) {
        if (areaId != null) {
            this.areaId = areaId;
        }

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

    public Long getAreaId() {
        return areaId;
    }

    public String getName() {
        return name;
    }
}
