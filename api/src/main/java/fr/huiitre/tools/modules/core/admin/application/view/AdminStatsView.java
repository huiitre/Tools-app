package fr.huiitre.tools.modules.core.admin.application.view;

import java.util.List;

public record AdminStatsView(
        long totalUsers,
        long activeUsers,
        long newUsersThisWeek,
        List<ModuleUserCount> usersPerModule) {

    public record ModuleUserCount(String moduleCode, String moduleName, long userCount) {}
}
