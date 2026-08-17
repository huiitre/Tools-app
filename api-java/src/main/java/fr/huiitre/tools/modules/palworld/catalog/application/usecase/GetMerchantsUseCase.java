package fr.huiitre.tools.modules.palworld.catalog.application.usecase;

import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.catalog.application.ports.ShopCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.view.MerchantView;
import fr.huiitre.tools.modules.palworld.catalog.application.view.ShopCurrencyView;

@Service
public class GetMerchantsUseCase implements SecuredUseCase {

    // Ces 3 devises spéciales (arène, primes, médailles) ne sont jamais "vendues" et n'ont donc aucune ligne
    // dans tools_palworld.item — aucun nom/icône disponible côté extracteur, cf. web/AGENTS.md section
    // Marchands. Libellés codés en dur en dernier recours, explicitement autorisé plutôt que d'attendre une
    // extraction dédiée.
    private static final Map<String, String> CURRENCY_FALLBACK_NAMES = Map.of(
            "BattleTicket", "Ticket de combat",
            "DogCoin", "Médaille de chien",
            "BountyProof_1", "Preuve de prime");

    private final ShopCatalogRepository shopCatalogRepository;

    public GetMerchantsUseCase(ShopCatalogRepository shopCatalogRepository) {
        this.shopCatalogRepository = shopCatalogRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public List<MerchantView> execute() {
        return shopCatalogRepository.findAllMerchants().stream().map(this::withResolvedCurrency).toList();
    }

    private MerchantView withResolvedCurrency(MerchantView merchant) {
        ShopCurrencyView currency = merchant.currency();
        // Priorité au libellé codé en dur pour ces 3 devises connues, même si tools_palworld.item contient
        // déjà une ligne du même slug (ex: "DogCoin" droppé par un Pal, avec un nom brut non traduit) :
        // le nom de référence pour un item vendu par un Pal n'a rien à voir avec le nom d'affichage voulu
        // pour une devise de marchand.
        String hardcodedName = CURRENCY_FALLBACK_NAMES.get(currency.itemId());
        if (hardcodedName == null && currency.name() != null) return merchant;
        String resolvedName = hardcodedName != null ? hardcodedName : currency.itemId();
        return new MerchantView(
                merchant.id(), merchant.externalId(), merchant.code(), merchant.name(), merchant.portraitUrl(),
                merchant.restockMinute(), new ShopCurrencyView(currency.itemId(), resolvedName, currency.iconUrl()),
                merchant.offers());
    }
}
