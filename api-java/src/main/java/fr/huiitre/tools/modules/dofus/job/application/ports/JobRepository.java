package fr.huiitre.tools.modules.dofus.job.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.dofus.job.domain.Job;

public interface JobRepository {

    List<Job> findAllByGameVersionId(Long gameVersionId);

    Long insert(Job job);

    void update(Job job);
}
