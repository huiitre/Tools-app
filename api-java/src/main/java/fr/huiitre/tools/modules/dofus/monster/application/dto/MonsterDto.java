package fr.huiitre.tools.modules.dofus.monster.application.dto;

import java.util.List;

public class MonsterDto {
    
    private final Long id;
    private final String name;
    private final List<MonsterImageDto> images;
    private final boolean boss;


    public MonsterDto(
        Long id,
        String name,
        List<MonsterImageDto> images,
        boolean boss
    ) {
        this.id = id;
        this.name = name;
        this.images = images;
        this.boss = boss;
    }

    public Long getId() {
        return id;
    }

    public String getName() {
        return name;
    }

    public List<MonsterImageDto> getImages() {
        return images;
    }

    public boolean isBoss() {
        return boss;
    }
}
