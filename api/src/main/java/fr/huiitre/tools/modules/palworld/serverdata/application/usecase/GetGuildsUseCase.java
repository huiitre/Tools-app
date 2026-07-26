package fr.huiitre.tools.modules.palworld.serverdata.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.GuildQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.GuildSummaryView;

@Service
public class GetGuildsUseCase implements SecuredUseCase {

    private final GuildQueryRepository guildQueryRepository;

    public GetGuildsUseCase(GuildQueryRepository guildQueryRepository) {
        this.guildQueryRepository = guildQueryRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public List<GuildSummaryView> execute() {
        return guildQueryRepository.findAllWithMembersAndBases();
    }
}
