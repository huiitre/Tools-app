package fr.huiitre.tools.modules.core.user.api;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.auth.api.dto.UserProfileDto;
import fr.huiitre.tools.modules.core.auth.application.usecase.SetUserPasswordUseCase;
import fr.huiitre.tools.modules.core.user.application.usecase.GetCurrentUserProfileUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;

@Tag(name = "Core - User")
@RestController
@RequestMapping("/user")
public class UserController {

    private final GetCurrentUserProfileUseCase getCurrentUserProfileUseCase;

    private final SetUserPasswordUseCase setUserPasswordUseCase;

    public UserController(
            GetCurrentUserProfileUseCase getCurrentUserProfileUseCase, SetUserPasswordUseCase setUserPasswordUseCase) {
        this.getCurrentUserProfileUseCase = getCurrentUserProfileUseCase;
        this.setUserPasswordUseCase = setUserPasswordUseCase;
    }

    @GetMapping("/me")
    public ResponseEntity<UserProfileDto> me() {

        return ResponseEntity.ok(getCurrentUserProfileUseCase.execute());
    }

    @PatchMapping("/password")
    public void setPassword(@Valid @RequestBody SetPasswordRequest request) {
        setUserPasswordUseCase.execute(request.password());
    }

    public record SetPasswordRequest(@NotBlank String password) {}
}