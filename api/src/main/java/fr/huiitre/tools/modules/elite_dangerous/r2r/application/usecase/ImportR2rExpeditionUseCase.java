package fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase;

import java.io.IOException;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.command.ImportR2rExpeditionCommand;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.importer.RouteImporter;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.ports.R2rExpeditionRepository;
import fr.huiitre.tools.modules.elite_dangerous.r2r.domain.R2rExpedition;

@Service
@Transactional
public class ImportR2rExpeditionUseCase implements SecuredUseCase {

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.ELITE_DANGEROUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    private final List<RouteImporter> importers;
    private final R2rExpeditionRepository repository;
    private final AuthenticatedUserProvider authenticatedUserProvider;

    public ImportR2rExpeditionUseCase(
            List<RouteImporter> importers,
            R2rExpeditionRepository repository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.importers = importers;
        this.repository = repository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public UUID execute(ImportR2rExpeditionCommand command) throws IOException {
        Long userId = authenticatedUserProvider.getUserId();

        RouteImporter importer = importers.stream()
                .filter(i -> i.source().equals(command.getSource()))
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException("R2R_UNKNOWN_SOURCE"));

        String routeData = importer.parse(command.getFileContent(), command.getFileName());
        String name = resolveName(command.getName(), command.getFileName(), userId);

        R2rExpedition expedition = R2rExpedition.create(routeData, name, command.getSource());
        return repository.save(userId, expedition);
    }

    private String resolveName(String providedName, String fileName, Long userId) {
        if (providedName != null && !providedName.isBlank()) {
            return providedName.trim();
        }
        if (fileName != null && !fileName.isBlank()) {
            int dot = fileName.lastIndexOf('.');
            String base = dot > 0 ? fileName.substring(0, dot) : fileName;
            if (!base.isBlank()) return base;
        }
        int count = repository.countByUserId(userId);
        return "Expédition #" + (count + 1);
    }
}
