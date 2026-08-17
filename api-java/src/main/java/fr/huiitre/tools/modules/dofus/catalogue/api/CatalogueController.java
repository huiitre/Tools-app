package fr.huiitre.tools.modules.dofus.catalogue.api;

import java.util.List;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.ModelAttribute;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.dofus.catalogue.api.dto.CatalogueSearchQuery;
import fr.huiitre.tools.modules.dofus.catalogue.application.dto.CatalogueColumnDto;
import fr.huiitre.tools.modules.dofus.catalogue.application.dto.CatalogueSearchResponse;
import fr.huiitre.tools.modules.dofus.catalogue.application.usecase.GetCatalogueColumnsUseCase;
import fr.huiitre.tools.modules.dofus.catalogue.application.usecase.GetCatalogueRecipeItemUseCase;
import fr.huiitre.tools.modules.dofus.catalogue.application.usecase.SearchCatalogueItemsUseCase;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Catalogue")
@RestController
@RequestMapping("/dofus/catalogue")
public class CatalogueController {

    private final GetCatalogueRecipeItemUseCase getCatalogueRecipeItemUseCase;

    private final GetCatalogueColumnsUseCase getCatalogueColumnsUseCase;
    private final SearchCatalogueItemsUseCase searchCatalogueItemsUseCase;

    public CatalogueController(GetCatalogueColumnsUseCase getCatalogueColumnsUseCase,
            SearchCatalogueItemsUseCase searchCatalogueItemsUseCase,
            GetCatalogueRecipeItemUseCase getCatalogueRecipeItemUseCase) {
        this.getCatalogueColumnsUseCase = getCatalogueColumnsUseCase;
        this.searchCatalogueItemsUseCase = searchCatalogueItemsUseCase;
        this.getCatalogueRecipeItemUseCase = getCatalogueRecipeItemUseCase;
    }

    @GetMapping("/search")
    public ResponseEntity<CatalogueSearchResponse> searchItems(
            @ModelAttribute CatalogueSearchQuery query,
            @RequestHeader(value = "X-Game-Serve-Id") Long gameServerId) {

        CatalogueSearchResponse response = searchCatalogueItemsUseCase.execute(query, gameServerId);

        return ResponseEntity.ok(response);
    }

    @GetMapping("/columns")
    public ResponseEntity<List<CatalogueColumnDto>> getColumns() {
        return ResponseEntity.ok(
                getCatalogueColumnsUseCase.execute());
    }

    @GetMapping("/item/{itemId}/recipe")
    public ResponseEntity<List<ItemDto>> getItemRecipe(
            @PathVariable Long itemId,
            @RequestHeader(value = "X-Game-Serve-Id") Long gameServerId) {
        return ResponseEntity.ok(
                getCatalogueRecipeItemUseCase.execute(itemId, gameServerId));
    }
}
