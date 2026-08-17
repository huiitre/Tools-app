package fr.huiitre.tools.modules.dofus.workshop.application.repository;

import java.util.List;
import java.util.Map;
import java.util.Optional;

import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItem;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopLink;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

public interface WorkshopRepository {
    
    boolean existsByUserIdAndName(Long userId, String name);

    boolean existsByIdAndUserId(Long userId, Long workshopId);

    Long create(Long gameVersionId, Long userId, Workshop workshop);

    void update(Long userId, Workshop workshop);

    void delete(Long userId, Long workshopId);

    Optional<Workshop> findByIdAndUserId(Long userId, Long workshopId);

    List<Workshop> findAllByUserIdAndGameVersionId(Long userId, Long gameVersionId);

    List<WorkshopTag> findAllTagsByUserIdAndWorkshopId(Long userId, Long workshopId);

    List<WorkshopItem> findAllItemsByUserIdAndWorkshopId(Long userId, Long workshopId);

    List<WorkshopItemIngredient> findAllIngredientsByUserIdAndWorkshopItemId(Long userId, Long workshopItemId);

    List<WorkshopLink> findAllLinksByUserIdAndWorkshopId(Long userId, Long workshopId);

    Map<Long, List<WorkshopLink>> findAllLinksByUserIdGroupedByWorkshopId(Long userId);

    Long addItemToWorkshop(Long userId, Long workshopId, WorkshopItem workshopItem);

    void addIngredients(Long userId, List<WorkshopItemIngredient> ingredients);

    void deleteWorkshopItem(Long userId, Long workshopId, Long workshopItemId);

    void updateWorkshopItemQuantity(Long userId, Long workshopId, Long workshopItemId, Long quantity);

    Optional<WorkshopItemIngredient> findIngredientByIdAndUserId(Long userId, Long ingredientId);

    List<WorkshopItemIngredient> findIngredientsByParentIngredientId(Long userId, Long parentIngredientId);

    void updateIngredientQuantityObtained(Long userId, Long ingredientId, Long quantityObtained);

    void deleteIngredientsByParentId(Long userId, Long parentIngredientId);

    void addTagsToWorkshop(Long userId, Long workshopId, List<Long> tagIds);

    void removeTagFromWorkshop(Long userId, Long workshopId, Long tagId);

    Long addLink(Long userId, Long workshopId, WorkshopLink link);

    Optional<WorkshopLink> findLinkByIdAndWorkshopId(Long userId, Long workshopId, Long linkId);

    void updateLink(Long userId, Long workshopId, Long linkId, String url, String label);

    int deleteLink(Long userId, Long workshopId, Long linkId);

    Optional<Workshop> findById(Long id, Long gameVersionId);

    Long findOwnerUserId(Long workshopId);
}
