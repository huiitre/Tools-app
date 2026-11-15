package fr.huiitre.tools.modules.dofus.monster.domain;

public class Monster {
    
    private final Long id;
    private final Long assetId;
    private final Long gameVersionId;
    private String name;
    private boolean boss;

    private Monster(
            Long id,
            Long assetId,
            Long gameVersionId,
            String name,
            boolean boss) {
        this.id = id;
        this.assetId = assetId;
        this.gameVersionId = gameVersionId;
        this.name = name;
        this.boss = boss;
    }

    public static Monster rehydrate(
            Long id,
            Long assetId,
            Long gameVersionId,
            String name,
            boolean boss) {
        if (id == null) {
            throw new IllegalArgumentException("MONSTER_ID_REQUIRED");
        }

        return new Monster(
                id,
                assetId,
                gameVersionId,
                name,
                boss);
    }

    public static Monster create(
            Long assetId,
            Long gameVersionId,
            String name,
            boolean boss) {
        return new Monster(
                null,
                assetId,
                gameVersionId,
                name,
                boss);
    }

    public void update(
            String name,
            boolean boss) {
        if (name != null && !name.isBlank()) {
            this.name = name;
        }
        this.boss = boss;
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

    public boolean isBoss() {
        return boss;
    }
}
