package fr.huiitre.tools.modules.dofus.workshop.application.repository;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

public interface WorkshopTagRepository {
    
    boolean existsByUserIdAndGameVersionIdAndName(Long userId, Long gameVersionId, String name);

    //* Même nom ailleurs dans la version du jeu de tagId, en dehors de lui-même.
    boolean existsOtherWithNameInSameGameVersion(Long userId, Long tagId, String name);

    boolean existsByIdAndUserId(Long userId, Long tagId);

    Long create(Long gameVersionId, Long userId, WorkshopTag tag);

    void update(Long userId, WorkshopTag tag);

    void delete(Long userId, Long tagId);

    Optional<WorkshopTag> findByIdAndUserId(Long userId, Long tagId);

    List<WorkshopTag> findAllByUserIdAndGameVersionId(Long userId, Long gameVersionId);
}
