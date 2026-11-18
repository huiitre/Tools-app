package fr.huiitre.tools.modules.dofus.job.domain;

import java.util.Objects;

public class Job {

    private final Long id;
    private final Long assetId;
    private final Long gameVersionId;
    private String name;
    private Long iconId;

    private Job(
            Long id,
            Long assetId,
            Long gameVersionId,
            String name,
            Long iconId) {
        this.id = id;
        this.assetId = assetId;
        this.gameVersionId = gameVersionId;
        this.name = name;
        this.iconId = iconId;
    }

    public static Job rehydrate(
            Long id,
            Long assetId,
            Long gameVersionId,
            String name,
            Long iconId) {
        if (id == null) {
            throw new IllegalArgumentException("JOB_ID_REQUIRED");
        }

        return new Job(id, assetId, gameVersionId, name, iconId);
    }

    public static Job create(
            Long assetId,
            Long gameVersionId,
            String name,
            Long iconId) {
        return new Job(null, assetId, gameVersionId, name, iconId);
    }

    public void update(String name, Long iconId) {
        if (name != null && !name.isBlank()) {
            this.name = name;
        }
        this.iconId = iconId;
    }

    public boolean differsFrom(String name, Long iconId) {
        return !this.name.equals(name) || !Objects.equals(this.iconId, iconId);
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

    public Long getIconId() {
        return iconId;
    }
}
