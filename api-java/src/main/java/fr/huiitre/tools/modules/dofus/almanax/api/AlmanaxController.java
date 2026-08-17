package fr.huiitre.tools.modules.dofus.almanax.api;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.almanax.application.command.AddAlmanaxSubscriptionCommand;
import fr.huiitre.tools.modules.dofus.almanax.application.usecase.AddAlmanaxSubscriptionUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.usecase.AlmanaxSubscriptionNotifier;
import fr.huiitre.tools.modules.dofus.almanax.application.usecase.GetAlmanaxCalendarUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.usecase.GetMyAlmanaxSubscriptionsUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.usecase.RemoveAlmanaxSubscriptionUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.view.AlmanaxDto;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Almanax")
@RestController
@RequestMapping("/dofus/almanax")
public class AlmanaxController {

    private final GetAlmanaxCalendarUseCase getAlmanaxCalendarUseCase;
    private final GetMyAlmanaxSubscriptionsUseCase getMySubscriptionsUseCase;
    private final AddAlmanaxSubscriptionUseCase addSubscriptionUseCase;
    private final RemoveAlmanaxSubscriptionUseCase removeSubscriptionUseCase;
    private final AlmanaxSubscriptionNotifier subscriptionNotifier;

    public AlmanaxController(
            GetAlmanaxCalendarUseCase getAlmanaxCalendarUseCase,
            GetMyAlmanaxSubscriptionsUseCase getMySubscriptionsUseCase,
            AddAlmanaxSubscriptionUseCase addSubscriptionUseCase,
            RemoveAlmanaxSubscriptionUseCase removeSubscriptionUseCase,
            AlmanaxSubscriptionNotifier subscriptionNotifier) {
        this.getAlmanaxCalendarUseCase = getAlmanaxCalendarUseCase;
        this.getMySubscriptionsUseCase = getMySubscriptionsUseCase;
        this.addSubscriptionUseCase = addSubscriptionUseCase;
        this.removeSubscriptionUseCase = removeSubscriptionUseCase;
        this.subscriptionNotifier = subscriptionNotifier;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping
    public ResponseEntity<List<AlmanaxDto>> getAllAlmanax() {
        return ResponseEntity.ok(getAlmanaxCalendarUseCase.execute());
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/subscriptions")
    public List<Long> getMySubscriptions() {
        return getMySubscriptionsUseCase.execute();
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping("/subscriptions")
    @ResponseStatus(HttpStatus.CREATED)
    public void addSubscription(@RequestBody AddAlmanaxSubscriptionCommand command) {
        addSubscriptionUseCase.execute(command);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/subscriptions/{almanaxId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void removeSubscription(@PathVariable Long almanaxId) {
        removeSubscriptionUseCase.execute(almanaxId);
    }

    @RequiredRole(RoleCode.ADMIN)
    @PostMapping("/subscriptions/admin/sync")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void triggerSync() {
        subscriptionNotifier.processToday();
    }
}
