package fr.huiitre.tools.modules.elite_dangerous.r2r.api;

import java.io.IOException;
import java.util.List;
import java.util.UUID;

import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestPart;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.command.ImportR2rExpeditionCommand;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.command.RenameR2rExpeditionCommand;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.command.UpdateR2rProgressCommand;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase.DeleteR2rExpeditionUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase.ExportR2rExpeditionUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase.GetR2rExpeditionUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase.GetR2rExpeditionsUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase.ImportR2rExpeditionUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase.RenameR2rExpeditionUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.usecase.UpdateR2rProgressUseCase;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.view.R2rExpeditionDetailView;
import fr.huiitre.tools.modules.elite_dangerous.r2r.application.view.R2rExpeditionSummaryView;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Elite Dangerous - Road to Riches")
@RestController
@RequestMapping("/elite-dangerous/r2r")
public class R2rController {

    private final GetR2rExpeditionsUseCase getR2rExpeditionsUseCase;
    private final GetR2rExpeditionUseCase getR2rExpeditionUseCase;
    private final ImportR2rExpeditionUseCase importR2rExpeditionUseCase;
    private final UpdateR2rProgressUseCase updateR2rProgressUseCase;
    private final RenameR2rExpeditionUseCase renameR2rExpeditionUseCase;
    private final ExportR2rExpeditionUseCase exportR2rExpeditionUseCase;
    private final DeleteR2rExpeditionUseCase deleteR2rExpeditionUseCase;

    public R2rController(
            GetR2rExpeditionsUseCase getR2rExpeditionsUseCase,
            GetR2rExpeditionUseCase getR2rExpeditionUseCase,
            ImportR2rExpeditionUseCase importR2rExpeditionUseCase,
            UpdateR2rProgressUseCase updateR2rProgressUseCase,
            RenameR2rExpeditionUseCase renameR2rExpeditionUseCase,
            ExportR2rExpeditionUseCase exportR2rExpeditionUseCase,
            DeleteR2rExpeditionUseCase deleteR2rExpeditionUseCase) {
        this.getR2rExpeditionsUseCase = getR2rExpeditionsUseCase;
        this.getR2rExpeditionUseCase = getR2rExpeditionUseCase;
        this.importR2rExpeditionUseCase = importR2rExpeditionUseCase;
        this.updateR2rProgressUseCase = updateR2rProgressUseCase;
        this.renameR2rExpeditionUseCase = renameR2rExpeditionUseCase;
        this.exportR2rExpeditionUseCase = exportR2rExpeditionUseCase;
        this.deleteR2rExpeditionUseCase = deleteR2rExpeditionUseCase;
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping("/expeditions")
    public List<R2rExpeditionSummaryView> getExpeditions() {
        return getR2rExpeditionsUseCase.execute();
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping("/expeditions/{id}")
    public R2rExpeditionDetailView getExpedition(@PathVariable UUID id) {
        return getR2rExpeditionUseCase.execute(id);
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping(value = "/expeditions", consumes = MediaType.MULTIPART_FORM_DATA_VALUE)
    @ResponseStatus(HttpStatus.CREATED)
    public UUID importExpedition(
            @RequestPart("file") MultipartFile file,
            @RequestPart("source") String source,
            @RequestPart(value = "name", required = false) String name) throws IOException {
        var command = new ImportR2rExpeditionCommand(file.getBytes(), file.getOriginalFilename(), source, name);
        return importR2rExpeditionUseCase.execute(command);
    }

    @RequiredRole(RoleCode.USER)
    @PatchMapping("/expeditions/{id}/progress")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void updateProgress(
            @PathVariable UUID id,
            @RequestBody UpdateR2rProgressCommand command) {
        updateR2rProgressUseCase.execute(id, command);
    }

    @RequiredRole(RoleCode.USER)
    @PatchMapping("/expeditions/{id}/name")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void renameExpedition(
            @PathVariable UUID id,
            @RequestBody RenameR2rExpeditionCommand command) {
        renameR2rExpeditionUseCase.execute(id, command);
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping("/expeditions/{id}/export")
    public ResponseEntity<String> exportExpedition(@PathVariable UUID id) {
        String routeData = exportR2rExpeditionUseCase.execute(id);
        return ResponseEntity.ok()
                .contentType(MediaType.APPLICATION_JSON)
                .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=\"expedition-" + id + ".json\"")
                .body(routeData);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/expeditions/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteExpedition(@PathVariable UUID id) {
        deleteR2rExpeditionUseCase.execute(id);
    }
}
