package fr.huiitre.tools.modules.riot.valorant.application.usecase;

import java.util.Map;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantVersionProvider;

@Service
public class GetValorantVersionUseCase implements SecuredUseCase {

    private final ValorantVersionProvider versionProvider;

    public GetValorantVersionUseCase(ValorantVersionProvider versionProvider) {
        this.versionProvider = versionProvider;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public Map<String, Object> execute() {
        return versionProvider.getVersion();
    }
}
