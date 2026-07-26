package fr.huiitre.tools.modules.palworld.catalog.api;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.catalog.application.usecase.GetPalsUseCase;
import fr.huiitre.tools.modules.palworld.catalog.application.view.PalListItemView;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Palworld - Catalog")
@RestController
@RequestMapping("/palworld/pals")
public class PalworldCatalogController {

    private final GetPalsUseCase getPalsUseCase;

    public PalworldCatalogController(GetPalsUseCase getPalsUseCase) {
        this.getPalsUseCase = getPalsUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping
    @ResponseStatus(HttpStatus.OK)
    public List<PalListItemView> getPals() {
        return getPalsUseCase.execute();
    }
}
