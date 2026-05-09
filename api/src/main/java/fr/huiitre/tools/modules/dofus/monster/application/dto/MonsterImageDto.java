package fr.huiitre.tools.modules.dofus.monster.application.dto;

public class MonsterImageDto {
    
    private final Long id;
    private final Long monsterId;
    private final String resolution;
    private final Long iconId;
    private String url;

    public MonsterImageDto(
            Long id,
            Long monsterId,
            String resolution,
            Long iconId) {
        this.id = id;
        this.monsterId = monsterId;
        this.resolution = resolution;
        this.iconId = iconId;
    }

    public Long getId() {
        return id;
    }

    public Long getMonsterId() {
        return monsterId;
    }

    public String getResolution() {
        return resolution;
    }

    public Long getIconId() {
        return iconId;
    }

    public String getUrl() {
        return url;
    }

    public void setUrl(String url) {
        this.url = url;
    }
}
