package fr.huiitre.tools.modules.palworld.catalog.api;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.catalog.application.usecase.GetMerchantsUseCase;
import fr.huiitre.tools.modules.palworld.catalog.application.view.MerchantView;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Palworld - Catalog")
@RestController
@RequestMapping("/palworld/shop")
public class PalworldShopController {

    private final GetMerchantsUseCase getMerchantsUseCase;

    public PalworldShopController(GetMerchantsUseCase getMerchantsUseCase) {
        this.getMerchantsUseCase = getMerchantsUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/merchants")
    @ResponseStatus(HttpStatus.OK)
    public List<MerchantView> getMerchants() {
        return getMerchantsUseCase.execute();
    }
}
