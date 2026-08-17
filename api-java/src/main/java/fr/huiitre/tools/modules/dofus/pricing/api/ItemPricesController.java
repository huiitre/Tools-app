package fr.huiitre.tools.modules.dofus.pricing.api;

import java.util.List;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.dofus.pricing.application.usecase.GetItemPricesUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;

import fr.huiitre.tools.modules.dofus.pricing.api.dto.ItemPricesBatchRequest;
import fr.huiitre.tools.modules.dofus.pricing.api.dto.ItemPricesRequest;
import fr.huiitre.tools.modules.dofus.pricing.application.usecase.UpdateItemPricesUseCase;
import fr.huiitre.tools.modules.dofus.pricing.application.view.ItemPriceDto;


@Tag(name = "Dofus - Item Prices")
@RestController
@RequestMapping("/dofus/item/prices")
public class ItemPricesController {
    
    private final GetItemPricesUseCase getItemPricesUseCase;
    private final UpdateItemPricesUseCase updateItemPricesUseCase;

    public ItemPricesController(GetItemPricesUseCase getItemPricesUseCase, UpdateItemPricesUseCase updateItemPricesUseCase) {
        this.getItemPricesUseCase = getItemPricesUseCase;
        this.updateItemPricesUseCase = updateItemPricesUseCase;
    }

    @PostMapping
    public ResponseEntity<List<ItemPriceDto>> getItemPrices(
        @RequestHeader(value = "X-Game-Serve-Id") Long gameServerId,
        @RequestBody ItemPricesRequest request
    ) {
        List<Long> itemIds = request == null || request.itemIds() == null ? List.of() : request.itemIds();
        return ResponseEntity.ok(getItemPricesUseCase.execute(gameServerId, itemIds));
    }

    @PutMapping("/batch")
    public ResponseEntity<Void> getItemPricesBatch(
        @RequestHeader(value = "X-Game-Serve-Id") Long gameServerId,
        @RequestBody List<ItemPricesBatchRequest> requests
    ) {
        updateItemPricesUseCase.execute(gameServerId, requests);
        return ResponseEntity.ok().build();
    }
}
