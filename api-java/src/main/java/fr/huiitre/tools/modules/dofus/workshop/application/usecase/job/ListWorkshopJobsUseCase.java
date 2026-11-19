package fr.huiitre.tools.modules.dofus.workshop.application.usecase.job;

import java.util.Comparator;
import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.job.application.ports.JobRepository;
import fr.huiitre.tools.modules.dofus.job.domain.Job;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopJobDto;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopDtoAssembler;

@Service
@Transactional(readOnly = true)
public class ListWorkshopJobsUseCase implements SecuredUseCase {

    private final JobRepository jobRepository;
    private final WorkshopDtoAssembler workshopDtoAssembler;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public ListWorkshopJobsUseCase(
        JobRepository jobRepository,
        WorkshopDtoAssembler workshopDtoAssembler
    ) {
        this.jobRepository = jobRepository;
        this.workshopDtoAssembler = workshopDtoAssembler;
    }

    public List<WorkshopJobDto> execute(Long gameVersionId) {
        return jobRepository.findAllByGameVersionId(gameVersionId).stream()
            .sorted(Comparator.comparing(Job::getName))
            .map(job -> workshopDtoAssembler.jobDto(job.getId(), job.getName(), job.getIconId()))
            .toList();
    }
}
