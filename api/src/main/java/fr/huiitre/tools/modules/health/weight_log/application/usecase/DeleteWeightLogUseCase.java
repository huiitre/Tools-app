package fr.huiitre.tools.modules.health.weight_log.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.health.weight_log.application.ports.WeightLogRepository;

@Service
@Transactional
public class DeleteWeightLogUseCase implements SecuredUseCase {

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.HEALTH);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    private final WeightLogRepository weightLogRepository;

    private final AuthenticatedUserProvider authenticatedUserProvider;

    public DeleteWeightLogUseCase(
            WeightLogRepository weightLogRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.weightLogRepository = weightLogRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public void execute(Long weightLogId) {

        Long userId = authenticatedUserProvider.getUserId();

        if (!weightLogRepository.existsById(userId, weightLogId)) {
            throw new IllegalArgumentException("WEIGHT_LOG_NOT_FOUND");
        }

        weightLogRepository.delete(userId, weightLogId);
    }
}