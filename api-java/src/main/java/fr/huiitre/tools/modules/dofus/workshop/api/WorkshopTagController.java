package fr.huiitre.tools.modules.dofus.workshop.api;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.CreateWorkshopTagRequest;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.UpdateWorkshopTagRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopTagDto;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag.CreateWorkshopTagUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag.DeleteWorkshopTagUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag.ListWorkshopTagsUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag.UpdateWorkshopTagUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Workshop Tag")
@RestController
@RequestMapping("/dofus/workshop/tags")
public class WorkshopTagController {

    private final CreateWorkshopTagUseCase createWorkshopTagUseCase;
    private final UpdateWorkshopTagUseCase updateWorkshopTagUseCase;
    private final DeleteWorkshopTagUseCase deleteWorkshopTagUseCase;
    private final ListWorkshopTagsUseCase listWorkshopTagsUseCase;

    public WorkshopTagController(
            CreateWorkshopTagUseCase createWorkshopTagUseCase,
            UpdateWorkshopTagUseCase updateWorkshopTagUseCase,
            DeleteWorkshopTagUseCase deleteWorkshopTagUseCase,
            ListWorkshopTagsUseCase listWorkshopTagsUseCase) {
        this.createWorkshopTagUseCase = createWorkshopTagUseCase;
        this.updateWorkshopTagUseCase = updateWorkshopTagUseCase;
        this.deleteWorkshopTagUseCase = deleteWorkshopTagUseCase;
        this.listWorkshopTagsUseCase = listWorkshopTagsUseCase;
    }

    @RequiredRole(RoleCode.USER)
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public WorkshopTagDto createWorkshopTag(
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId,
            @RequestBody CreateWorkshopTagRequest request) {
        return createWorkshopTagUseCase.execute(gameVersionId, request);
    }

    @RequiredRole(RoleCode.USER)
    @PatchMapping("/{tagId}")
    @ResponseStatus(HttpStatus.OK)
    public WorkshopTagDto updateWorkshopTag(
            @PathVariable Long tagId,
            @RequestBody UpdateWorkshopTagRequest request) {
        return updateWorkshopTagUseCase.execute(tagId, request);
    }

    @RequiredRole(RoleCode.USER)
    @DeleteMapping("/{tagId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteWorkshopTag(
            @PathVariable Long tagId) {
        deleteWorkshopTagUseCase.execute(tagId);
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping
    public ResponseEntity<List<WorkshopTagDto>> getWorkshopTags(
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId) {
        return ResponseEntity.ok(
                listWorkshopTagsUseCase.execute(gameVersionId));
    }
}
