package fr.huiitre.tools.modules.dofus.sync.application.job;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.function.Function;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.job.application.ports.JobRepository;
import fr.huiitre.tools.modules.dofus.job.domain.Job;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;

@Service
@Transactional
public class SyncJobUseCase implements SecuredUseCase {

    private final JobDataProvider jobDataProvider;
    private final JobRepository jobRepository;

    public SyncJobUseCase(
            JobDataProvider jobDataProvider,
            JobRepository jobRepository) {
        this.jobDataProvider = jobDataProvider;
        this.jobRepository = jobRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public SyncReport execute(GameVersionData gameVersion) {

        Map<Long, Job> currentByAssetId = jobRepository.findAllByGameVersionId(gameVersion.getId()).stream()
            .collect(Collectors.toMap(Job::getAssetId, Function.identity()));

        List<String> created = new ArrayList<>();
        List<String> updated = new ArrayList<>();

        for (JobSyncData ext : jobDataProvider.fetchAll()) {

            Job existing = currentByAssetId.get(ext.assetId());

            if (existing == null) {
                jobRepository.insert(Job.create(ext.assetId(), gameVersion.getId(), ext.name(), ext.iconId()));

                created.add("assetId=" + ext.assetId() + " name=\"" + ext.name() + "\" iconId=" + ext.iconId());
                continue;
            }

            if (existing.differsFrom(ext.name(), ext.iconId())) {
                String before = "name=\"" + existing.getName() + "\" iconId=" + existing.getIconId();
                existing.update(ext.name(), ext.iconId());
                jobRepository.update(existing);

                updated.add("assetId=" + ext.assetId() + " " + before + " -> name=\"" + ext.name() + "\" iconId=" + ext.iconId());
            }
        }

        return new SyncReport("jobs", "Jobs", created, updated);
    }
}
