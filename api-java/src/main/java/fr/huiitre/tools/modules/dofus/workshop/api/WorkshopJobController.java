package fr.huiitre.tools.modules.dofus.workshop.api;

import java.util.List;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopJobDto;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.job.ListWorkshopJobsUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Workshop Job")
@RestController
@RequestMapping("/dofus/workshop/jobs")
public class WorkshopJobController {

    private final ListWorkshopJobsUseCase listWorkshopJobsUseCase;

    public WorkshopJobController(ListWorkshopJobsUseCase listWorkshopJobsUseCase) {
        this.listWorkshopJobsUseCase = listWorkshopJobsUseCase;
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping
    public List<WorkshopJobDto> getWorkshopJobs(
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId) {
        return listWorkshopJobsUseCase.execute(gameVersionId);
    }
}
