package fr.huiitre.tools.modules.dofus.workshop.application.usecase.item;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;

@Service
@Transactional
public class UncraftIngredientUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public UncraftIngredientUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
    }

    public void execute(Long workshopId, Long ingredientId) {
        Long userId = authenticatedUserProvider.getUserId();

        boolean exists = workshopRepository.existsByIdAndUserId(userId, workshopId);
        if (!exists) {
            throw new WorkshopNotFoundException();
        }

        // Vérifier que l'ingrédient a bien des sous-ingrédients (= a été crafté)
        List<WorkshopItemIngredient> subIngredients = workshopRepository.findIngredientsByParentIngredientId(userId, ingredientId);
        
        if (subIngredients.isEmpty()) {
            throw new IllegalArgumentException("Ingredient has not been crafted");
        }

        workshopRepository.deleteIngredientsByParentId(userId, ingredientId);
        workshopRepository.updateIngredientQuantityObtained(userId, ingredientId, 0L);
    }
}