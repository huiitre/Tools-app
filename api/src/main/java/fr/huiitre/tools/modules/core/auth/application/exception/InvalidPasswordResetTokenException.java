package fr.huiitre.tools.modules.core.auth.application.exception;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.ResponseStatus;

import fr.huiitre.tools.modules.core.common.application.exception.ApplicationException;

@ResponseStatus(HttpStatus.BAD_REQUEST)
public class InvalidPasswordResetTokenException extends ApplicationException {

    public InvalidPasswordResetTokenException(String message) {
        super(message);
    }

}
