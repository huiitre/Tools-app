package fr.huiitre.tools.modules.core.security.application.exception;

import fr.huiitre.tools.modules.core.common.application.exception.ApplicationException;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;

public class ForbiddenException extends ApplicationException {

    public ForbiddenException(String message) {
        super(message);
    }

    public ForbiddenException(ModuleCode moduleCode) {
        super("Access forbidden for module: " + moduleCode.name());
    }
}
