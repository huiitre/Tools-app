package fr.huiitre.tools.modules.dofus.item.api;

import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.ModelAttribute;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.dofus.item.api.dto.ItemMetadataBatchQuery;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemLightDTO;
import fr.huiitre.tools.modules.dofus.item.application.usecase.GetItemsMetadataBatchUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Items")
@RestController
@RequestMapping("/dofus/item")
public class ItemController {

    private final static Logger logger = LoggerFactory.getLogger(ItemController.class);

    private final GetItemsMetadataBatchUseCase getItemsMetadataBatchUseCase;

    public ItemController(GetItemsMetadataBatchUseCase getItemsMetadataBatchUseCase) {
        this.getItemsMetadataBatchUseCase = getItemsMetadataBatchUseCase;
    }
  
    @GetMapping("/metadata/batch")
    public ResponseEntity<List<ItemLightDTO>> getItemsMetadataBatch(
        @RequestHeader(value = "X-Game-Version-Id") Long gameVersionId,
        @ModelAttribute ItemMetadataBatchQuery query
    ) {

        logger.debug("Ligne #34 || gameVersionId : {}", gameVersionId);

        return ResponseEntity.ok(
            this.getItemsMetadataBatchUseCase.execute(gameVersionId, query.assetIds())
        );
    }
}
