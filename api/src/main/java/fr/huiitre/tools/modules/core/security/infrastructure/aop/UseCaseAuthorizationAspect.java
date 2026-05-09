package fr.huiitre.tools.modules.core.security.infrastructure.aop;

import java.util.Optional;

import org.aspectj.lang.ProceedingJoinPoint;
import org.aspectj.lang.annotation.Around;
import org.aspectj.lang.annotation.Aspect;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.exception.ForbiddenException;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.ports.ModuleAuthorizationPort;
import fr.huiitre.tools.modules.core.security.application.ports.UserRoleProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.security.infrastructure.RoleHierarchy;

@Aspect
public class UseCaseAuthorizationAspect {

    private final ModuleAuthorizationPort moduleAuthorizationPort;
    private final UserRoleProvider userRoleProvider;
    private final CurrentUserProvider currentUserProvider;

    private static final Logger logger = LoggerFactory.getLogger(UseCaseAuthorizationAspect.class);

    public UseCaseAuthorizationAspect(
            ModuleAuthorizationPort moduleAuthorizationPort,
            UserRoleProvider userRoleProvider,
            CurrentUserProvider currentUserProvider) {
        this.moduleAuthorizationPort = moduleAuthorizationPort;
        this.userRoleProvider = userRoleProvider;
        this.currentUserProvider = currentUserProvider;
    }

    @Around("execution(* *.execute(..)) && this(securedUseCase)")
    public Object authorize(
            ProceedingJoinPoint pjp,
            SecuredUseCase securedUseCase) throws Throwable {

        String userId = currentUserProvider.getCurrentUserId();

        if (userId == null || "anonymousUser".equals(userId)) {
            return pjp.proceed();
        }

        Optional<ModuleCode> moduleOpt = securedUseCase.requiredModule();
        RoleCode requiredRole = securedUseCase.requiredRole();

        // 1) Vérification accès module (si module présent)
        if (moduleOpt.isPresent()) {
            if (!moduleAuthorizationPort.hasAccess(userId, moduleOpt.get())) {
                throw new ForbiddenException(moduleOpt.get());
            }
        }

        // 2) Récupération rôle effectif
        RoleCode actualRole = userRoleProvider.getUserRole(userId, moduleOpt.orElse(null));

        // 3) Comparaison hiérarchique
        if (!RoleHierarchy.hasAtLeast(actualRole, requiredRole)) {
            throw new ForbiddenException(
                    "Required role " + requiredRole + ", but was " + actualRole);
        }

        return pjp.proceed();
    }
}
