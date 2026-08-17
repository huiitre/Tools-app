package fr.huiitre.tools.modules.health.weight_log.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.health.weight_log.application.ports.WeightLogRepository;
import fr.huiitre.tools.modules.health.weight_log.application.view.WeightLogView;

@Service
@Transactional
public class GetAllMyWeightLogUseCase implements SecuredUseCase {

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.HEALTH);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final WeightLogRepository weightLogRepository;

    public GetAllMyWeightLogUseCase(
            WeightLogRepository weightLogRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.weightLogRepository = weightLogRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public List<WeightLogView> execute() {

        Long userId = authenticatedUserProvider.getUserId();

        return weightLogRepository.findAllByUserId(userId);
    }
}