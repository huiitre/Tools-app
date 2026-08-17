package fr.huiitre.tools.modules.health.weight_log.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.health.weight_log.application.command.CreateWeightLogCommand;
import fr.huiitre.tools.modules.health.weight_log.application.ports.WeightLogRepository;
import fr.huiitre.tools.modules.health.weight_log.domain.WeightLog;

@Service
@Transactional
public class CreateWeightLogUseCase implements SecuredUseCase {

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

    public CreateWeightLogUseCase(
            WeightLogRepository weightLogRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.weightLogRepository = weightLogRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public void execute(
            CreateWeightLogCommand command) {

        Long userId = authenticatedUserProvider.getUserId();

        WeightLog weightLog = new WeightLog(
                command.getWeight(),
                command.getLogDate(),
                command.getNotes());

        weightLogRepository.save(userId, weightLog);
    }
}
