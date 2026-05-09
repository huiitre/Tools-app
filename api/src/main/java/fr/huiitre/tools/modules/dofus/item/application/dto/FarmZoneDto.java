package fr.huiitre.tools.modules.dofus.item.application.dto;

import java.util.List;

import fr.huiitre.tools.modules.dofus.area.application.dto.AreaDto;
import fr.huiitre.tools.modules.dofus.monster.application.dto.MonsterDto;
import fr.huiitre.tools.modules.dofus.subarea.application.dto.SubareaDto;

public class FarmZoneDto {

    private final AreaDto area;
    private final SubareaDto subarea;
    private final List<MonsterDto> monsters;
    private final boolean isPrimary;

    public FarmZoneDto(
            AreaDto area,
            SubareaDto subarea,
            List<MonsterDto> monsters,
            boolean isPrimary) {
        this.area = area;
        this.subarea = subarea;
        this.monsters = monsters;
        this.isPrimary = isPrimary;
    }

    public AreaDto getArea() {
        return area;
    }

    public SubareaDto getSubarea() {
        return subarea;
    }

    public List<MonsterDto> getMonsters() {
        return monsters;
    }

    public boolean isPrimary() {
        return isPrimary;
    }

}
