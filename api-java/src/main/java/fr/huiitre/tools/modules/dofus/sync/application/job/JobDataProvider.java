package fr.huiitre.tools.modules.dofus.sync.application.job;

import java.util.List;

public interface JobDataProvider {

    List<JobSyncData> fetchAll();
}
