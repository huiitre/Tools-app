package fr.huiitre.tools.modules.riot.valorant.application.user.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.command.AddSkinToStoreHistoryCommand;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantStoreHistoryRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.util.Optional;

@Service
@Transactional
public class AddSkinToStoreHistoryUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final ValorantAuthRepository valorantAuthRepository;
    private final ValorantStoreHistoryRepository storeHistoryRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public AddSkinToStoreHistoryUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ValorantAuthRepository valorantAuthRepository,
            ValorantStoreHistoryRepository storeHistoryRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.valorantAuthRepository = valorantAuthRepository;
        this.storeHistoryRepository = storeHistoryRepository;
    }

    public void execute(AddSkinToStoreHistoryCommand command) {
        Long accountId = command.getAccountId();
        if (!valorantAuthRepository.existsByIdAndUserId(accountId, authenticatedUserProvider.getUserId())) {
            throw new IllegalArgumentException("VALORANT_ACCOUNT_NOT_FOUND");
        }
        LocalDate seenAt = command.getSeenAt() != null ? command.getSeenAt() : LocalDate.now();

        if (command.getSkinIds() == null) return;

        for (Long skinId : command.getSkinIds()) {
            if (!storeHistoryRepository.existsByAccountIdAndSkinIdAndDate(accountId, skinId, seenAt)) {
                storeHistoryRepository.add(accountId, skinId, seenAt);
            }
        }
    }
}
