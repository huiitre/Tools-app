package fr.huiitre.tools.modules.palworld.serverdata.application.usecase;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.PalInstanceQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.PalInstanceSummaryView;

@Service
public class GetBasePalsUseCase implements SecuredUseCase {

    private final PalInstanceQueryRepository palInstanceQueryRepository;

    public GetBasePalsUseCase(PalInstanceQueryRepository palInstanceQueryRepository) {
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

    public List<PalInstanceSummaryView> execute(UUID baseId) {
        return palInstanceQueryRepository.findByBaseId(baseId);
    }
}
