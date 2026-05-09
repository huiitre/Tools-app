package fr.huiitre.tools.modules.dofus.workshop.api;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.AddWorkshopLinkRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.UpdateWorkshopLinkRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.CreateWorkshopRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.UpdateWorkshopRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDetailResponse;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopIngredientDetailDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopItemDetailDto;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.AddTagsToWorkshopUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.AddWorkshopLinkUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.DeleteWorkshopLinkUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.UpdateWorkshopLinkUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.CreateWorkshopUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.DeleteWorkshopUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.ListWorkshopsUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.RemoveTagFromWorkshopUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.UpdateWorkshopUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.AddItemsToWorkshopUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.CraftIngredientUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.DeleteWorkshopItemUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.GetWorkshopDetailUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.SearchCraftableItemsUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.UncraftIngredientUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.UpdateIngredientQuantityObtainedUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.item.UpdateWorkshopItemQuantityUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Workshop")
@RestController
@RequestMapping("/dofus/workshops")
public class WorkshopController {

    private final CreateWorkshopUseCase createWorkshopUseCase;
    private final UpdateWorkshopUseCase updateWorkshopUseCase;
    private final DeleteWorkshopUseCase deleteWorkshopUseCase;
    private final ListWorkshopsUseCase listWorkshopsUseCase;
    private final GetWorkshopDetailUseCase getWorkshopDetailUseCase;
    private final SearchCraftableItemsUseCase searchCraftableItemsUseCase;
    private final AddItemsToWorkshopUseCase addItemsToWorkshopUseCase;
    private final DeleteWorkshopItemUseCase deleteWorkshopItemUseCase;
    private final UpdateWorkshopItemQuantityUseCase updateWorkshopItemQuantityUseCase;
    private final CraftIngredientUseCase craftIngredientUseCase;
    private final UncraftIngredientUseCase uncraftIngredientUseCase;
    private final UpdateIngredientQuantityObtainedUseCase updateIngredientQuantityObtainedUseCase;
    private final AddTagsToWorkshopUseCase addTagsToWorkshopUseCase;
    private final RemoveTagFromWorkshopUseCase removeTagFromWorkshopUseCase;
    private final AddWorkshopLinkUseCase addWorkshopLinkUseCase;
    private final UpdateWorkshopLinkUseCase updateWorkshopLinkUseCase;
    private final DeleteWorkshopLinkUseCase deleteWorkshopLinkUseCase;

    public WorkshopController(
        CreateWorkshopUseCase createWorkshopUseCase,
        UpdateWorkshopUseCase updateWorkshopUseCase,
        DeleteWorkshopUseCase deleteWorkshopUseCase,
        ListWorkshopsUseCase listWorkshopsUseCase,
        GetWorkshopDetailUseCase getWorkshopDetailUseCase,
        SearchCraftableItemsUseCase searchCraftableItemsUseCase,
        AddItemsToWorkshopUseCase addItemsToWorkshopUseCase,
        DeleteWorkshopItemUseCase deleteWorkshopItemUseCase,
        UpdateWorkshopItemQuantityUseCase updateWorkshopItemQuantityUseCase,
        CraftIngredientUseCase craftIngredientUseCase,
        UncraftIngredientUseCase uncraftIngredientUseCase,
        UpdateIngredientQuantityObtainedUseCase updateIngredientQuantityObtainedUseCase,
        AddTagsToWorkshopUseCase addTagsToWorkshopUseCase,
        RemoveTagFromWorkshopUseCase removeTagFromWorkshopUseCase,
        AddWorkshopLinkUseCase addWorkshopLinkUseCase,
        UpdateWorkshopLinkUseCase updateWorkshopLinkUseCase,
        DeleteWorkshopLinkUseCase deleteWorkshopLinkUseCase
    ) {
        this.createWorkshopUseCase = createWorkshopUseCase;
        this.updateWorkshopUseCase = updateWorkshopUseCase;
        this.deleteWorkshopUseCase = deleteWorkshopUseCase;
        this.listWorkshopsUseCase = listWorkshopsUseCase;
        this.getWorkshopDetailUseCase = getWorkshopDetailUseCase;
        this.searchCraftableItemsUseCase = searchCraftableItemsUseCase;
        this.addItemsToWorkshopUseCase = addItemsToWorkshopUseCase;
        this.deleteWorkshopItemUseCase = deleteWorkshopItemUseCase;
        this.updateWorkshopItemQuantityUseCase = updateWorkshopItemQuantityUseCase;
        this.craftIngredientUseCase = craftIngredientUseCase;
        this.uncraftIngredientUseCase = uncraftIngredientUseCase;
        this.updateIngredientQuantityObtainedUseCase = updateIngredientQuantityObtainedUseCase;
        this.addTagsToWorkshopUseCase = addTagsToWorkshopUseCase;
        this.removeTagFromWorkshopUseCase = removeTagFromWorkshopUseCase;
        this.addWorkshopLinkUseCase = addWorkshopLinkUseCase;
        this.updateWorkshopLinkUseCase = updateWorkshopLinkUseCase;
        this.deleteWorkshopLinkUseCase = deleteWorkshopLinkUseCase;
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public WorkshopDto createWorkshop(
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId,
            @RequestBody CreateWorkshopRequest request) {
        return createWorkshopUseCase.execute(gameVersionId, request);
    }

    @RequiredRole(RoleCode.USER)
    @PatchMapping("/{workshopId}")
    @ResponseStatus(HttpStatus.OK)
    public WorkshopDto updateWorkshop(
            @PathVariable Long workshopId,
            @RequestBody UpdateWorkshopRequest request) {
        return updateWorkshopUseCase.execute(workshopId, request);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{workshopId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteWorkshop(
            @PathVariable Long workshopId) {
        deleteWorkshopUseCase.execute(workshopId);
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping
    public ResponseEntity<List<WorkshopDto>> getWorkshops(
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId) {
        return ResponseEntity.ok(
                listWorkshopsUseCase.execute(gameVersionId));
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping("/{workshopId}")
    public ResponseEntity<WorkshopDetailResponse> getWorkshopDetail(
            @PathVariable Long workshopId,
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId) {
        return ResponseEntity.ok(
                getWorkshopDetailUseCase.execute(workshopId, gameVersionId));
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping("/{workshopId}/items/search")
    public ResponseEntity<List<ItemDto>> searchCraftableItems(
            @PathVariable Long workshopId,
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId,
            @RequestParam(required = false) String query) {
        return ResponseEntity.ok(
                searchCraftableItemsUseCase.execute(gameVersionId, workshopId, query));
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping("/{workshopId}/items")
    @ResponseStatus(HttpStatus.CREATED)
    public List<WorkshopItemDetailDto> addItemsToWorkshop(
            @PathVariable Long workshopId,
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId,
            @RequestBody List<Long> itemIds) {
        return addItemsToWorkshopUseCase.execute(workshopId, gameVersionId, itemIds);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{workshopId}/items/{workshopItemId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteWorkshopItem(
            @PathVariable Long workshopId,
            @PathVariable Long workshopItemId) {
        deleteWorkshopItemUseCase.execute(workshopId, workshopItemId);
    }

    @RequiredRole(RoleCode.USER)
    @PatchMapping("/{workshopId}/items/{workshopItemId}/quantity/{quantity}")
    @ResponseStatus(HttpStatus.OK)
    public void updateWorkshopItemQuantity(
            @PathVariable Long workshopId,
            @PathVariable Long workshopItemId,
            @PathVariable Long quantity) {
        updateWorkshopItemQuantityUseCase.execute(workshopId, workshopItemId, quantity);
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping("/{workshopId}/items/{workshopItemId}/ingredients/{ingredientId}/craft")
    @ResponseStatus(HttpStatus.CREATED)
    public List<WorkshopIngredientDetailDto> craftIngredient(
            @PathVariable Long workshopId,
            @PathVariable Long workshopItemId,
            @PathVariable Long ingredientId,
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId) {
        return craftIngredientUseCase.execute(workshopId, workshopItemId, ingredientId, gameVersionId);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{workshopId}/ingredients/{ingredientId}/crafted")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void uncraftIngredient(
            @PathVariable Long workshopId,
            @PathVariable Long ingredientId) {
        uncraftIngredientUseCase.execute(workshopId, ingredientId);
    }

    @RequiredRole(RoleCode.USER)
    @PatchMapping("/{workshopId}/ingredients/{ingredientId}/obtained/{quantityObtained}")
    @ResponseStatus(HttpStatus.OK)
    public void updateIngredientQuantityObtained(
            @PathVariable Long workshopId,
            @PathVariable Long ingredientId,
            @PathVariable Long quantityObtained) {
        updateIngredientQuantityObtainedUseCase.execute(workshopId, ingredientId, quantityObtained);
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping("/{workshopId}/tags")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public WorkshopDto addTagsToWorkshop(
            @PathVariable Long workshopId,
            @RequestBody List<Long> tagIds) {
        return addTagsToWorkshopUseCase.execute(workshopId, tagIds);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{workshopId}/tags/{tagId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public WorkshopDto removeTagFromWorkshop(
            @PathVariable Long workshopId,
            @PathVariable Long tagId) {
        return removeTagFromWorkshopUseCase.execute(workshopId, tagId);
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping("/{workshopId}/links")
    @ResponseStatus(HttpStatus.CREATED)
    public WorkshopLinkDto addWorkshopLink(
            @PathVariable Long workshopId,
            @RequestBody AddWorkshopLinkRequest request) {
        return addWorkshopLinkUseCase.execute(workshopId, request);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{workshopId}/links/{linkId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteWorkshopLink(
            @PathVariable Long workshopId,
            @PathVariable Long linkId) {
        deleteWorkshopLinkUseCase.execute(workshopId, linkId);
    }

    @RequiredRole(RoleCode.USER)
    @PutMapping("/{workshopId}/links/{linkId}")
    public WorkshopLinkDto updateWorkshopLink(
            @PathVariable Long workshopId,
            @PathVariable Long linkId,
            @RequestBody UpdateWorkshopLinkRequest request) {
        return updateWorkshopLinkUseCase.execute(workshopId, linkId, request);
    }
}