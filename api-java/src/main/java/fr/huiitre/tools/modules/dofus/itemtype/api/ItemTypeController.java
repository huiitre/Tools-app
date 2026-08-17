package fr.huiitre.tools.modules.dofus.itemtype.api;

import java.util.List;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.dofus.itemtype.application.usecase.ListItemTypeUseCase;
import fr.huiitre.tools.modules.dofus.itemtype.application.view.ItemTypeDto;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Item Types")
@RestController
@RequestMapping("/dofus/item-types")
public class ItemTypeController {

    private final ListItemTypeUseCase listItemTypeUseCase;

    public ItemTypeController(ListItemTypeUseCase listItemTypeUseCase) {
        this.listItemTypeUseCase = listItemTypeUseCase;
    }

    @GetMapping
    public ResponseEntity<List<ItemTypeDto>> getItemTypes(
            @RequestHeader(value = "X-Game-Version-Id") Long gameVersionId
    ) {
        return ResponseEntity.ok(this.listItemTypeUseCase.execute(gameVersionId));
    }
}
