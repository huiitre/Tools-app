package fr.huiitre.tools.modules.palworld.serverdata.application.usecase;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.PalInstanceQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.PalInstanceSnapshotView;

@Service
public class GetPalInstanceHistoryUseCase implements SecuredUseCase {

    private final PalInstanceQueryRepository palInstanceQueryRepository;

    public GetPalInstanceHistoryUseCase(PalInstanceQueryRepository palInstanceQueryRepository) {
        this.palInstanceQueryRepository = palInstanceQueryRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public List<PalInstanceSnapshotView> execute(UUID instanceId) {
        return palInstanceQueryRepository.findHistoryByInstanceId(instanceId);
    }
}
