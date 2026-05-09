package fr.huiitre.tools.modules.dofus.almanax.api;

import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.dofus.almanax.application.usecase.GetAlmanaxCalendarUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.view.AlmanaxDto;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Almanax")
@RestController
@RequestMapping("/dofus/almanax")
public class AlmanaxController {

    private static final Logger logger = LoggerFactory.getLogger(AlmanaxController.class);

    private final GetAlmanaxCalendarUseCase getAlmanaxCalendarUseCase;

    public AlmanaxController(
            GetAlmanaxCalendarUseCase getAlmanaxCalendarUseCase) {
        this.getAlmanaxCalendarUseCase = getAlmanaxCalendarUseCase;
    }

    @GetMapping
    public ResponseEntity<List<AlmanaxDto>> getAllAlmanax() {
        return ResponseEntity.ok(getAlmanaxCalendarUseCase.execute());
    }
}
