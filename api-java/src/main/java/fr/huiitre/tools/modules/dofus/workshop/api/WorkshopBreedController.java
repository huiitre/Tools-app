package fr.huiitre.tools.modules.dofus.workshop.api;

import java.util.List;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopBreedDto;
import fr.huiitre.tools.modules.dofus.workshop.application.usecase.breed.ListWorkshopBreedsUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Workshop Breed")
@RestController
@RequestMapping("/dofus/workshop/breeds")
public class WorkshopBreedController {

    private final ListWorkshopBreedsUseCase listWorkshopBreedsUseCase;

    public WorkshopBreedController(ListWorkshopBreedsUseCase listWorkshopBreedsUseCase) {
        this.listWorkshopBreedsUseCase = listWorkshopBreedsUseCase;
    }

    @RequiredRole(RoleCode.USER)
    @GetMapping
    public List<WorkshopBreedDto> getWorkshopBreeds(
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId) {
        return listWorkshopBreedsUseCase.execute(gameVersionId);
    }
}
