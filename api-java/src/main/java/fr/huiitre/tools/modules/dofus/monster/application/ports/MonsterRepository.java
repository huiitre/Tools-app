package fr.huiitre.tools.modules.dofus.monster.application.ports;

import java.util.Collection;
import java.util.List;

import fr.huiitre.tools.modules.dofus.monster.application.dto.MonsterImageDto;
import fr.huiitre.tools.modules.dofus.monster.domain.Monster;

public interface MonsterRepository {
    
    List<Monster> findAllByGameVersionId(Long gameVersionId);

    void update(Monster monster);

    Long insert(Monster monster);

    boolean refreshImages(Long monsterId, Long iconId);

    boolean refreshSubareas(Long monsterId, Collection<Long> subareaIds);

    boolean refreshDrops(Long monsterId, Collection<Long> itemIds);

    List<MonsterImageDto> findImageByMonsterId(Long monsterId);

    List<MonsterImageDto> findImageByMonsterIds(Collection<Long> monsterIds);
}
