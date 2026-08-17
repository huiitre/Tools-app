package fr.huiitre.tools.modules.dofus.monster.application.dto;

import java.util.List;

public class MonsterDto {
    
    private final Long id;
    private final String name;
    private final List<MonsterImageDto> images;


    public MonsterDto(
        Long id,
        String name,
        List<MonsterImageDto> images
    ) {
        this.id = id;
        this.name = name;
        this.images = images;
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
}
