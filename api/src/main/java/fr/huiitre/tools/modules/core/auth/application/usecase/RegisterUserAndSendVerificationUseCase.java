package fr.huiitre.tools.modules.core.auth.application.usecase;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.auth.application.command.RegisterUserCommand;
import fr.huiitre.tools.modules.core.user.domain.User;

@Service
@Transactional
public class RegisterUserAndSendVerificationUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final RegisterUserUseCase registerUserUseCase;
    private final SendEmailVerificationUseCase sendEmailVerificationUseCase;

    public RegisterUserAndSendVerificationUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            RegisterUserUseCase registerUserUseCase,
            SendEmailVerificationUseCase sendEmailVerificationUseCase) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.registerUserUseCase = registerUserUseCase;
        this.sendEmailVerificationUseCase = sendEmailVerificationUseCase;
    }

    public void execute(RegisterUserCommand command) {
        User user = registerUserUseCase.execute(command);

        sendEmailVerificationUseCase.execute(
                user.getId(),
                user.getEmail());
    }
}