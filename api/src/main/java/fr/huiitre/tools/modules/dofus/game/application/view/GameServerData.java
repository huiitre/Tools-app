package fr.huiitre.tools.modules.dofus.game.application.view;

public class GameServerData {
    private final Long id;
    private final Long gameVersionId;
    private final String name;
    private final String code;

    public GameServerData(
            Long id,
            Long gameVersionId,
            String name,
            String code) {
        this.id = id;
        this.gameVersionId = gameVersionId;
        this.name = name;
        this.code = code;
    }

    public Long getId() {
        return id;
    }

    public Long getGameVersionId() {
        return gameVersionId;
    }

    public String getName() {
        return name;
    }

    public String getCode() {
        return code;
    }
}
