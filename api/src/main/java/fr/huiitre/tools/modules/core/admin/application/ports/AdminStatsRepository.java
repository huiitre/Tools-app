package fr.huiitre.tools.modules.core.admin.application.ports;

import fr.huiitre.tools.modules.core.admin.application.view.AdminStatsView;

public interface AdminStatsRepository {

    AdminStatsView getStats();
}
