package fr.huiitre.tools.modules.dofus.breed.domain;

public class Breed {

    private final Long id;
    private final Long assetId;
    private final Long gameVersionId;
    private String name;

    private Breed(
            Long id,
            Long assetId,
            Long gameVersionId,
            String name) {
        this.id = id;
        this.assetId = assetId;
        this.gameVersionId = gameVersionId;
        this.name = name;
    }

    public static Breed rehydrate(
            Long id,
            Long assetId,
            Long gameVersionId,
            String name) {
        if (id == null) {
            throw new IllegalArgumentException("BREED_ID_REQUIRED");
        }

        return new Breed(id, assetId, gameVersionId, name);
    }

    public static Breed create(
            Long assetId,
            Long gameVersionId,
            String name) {
        return new Breed(null, assetId, gameVersionId, name);
    }

    public void update(String name) {
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
